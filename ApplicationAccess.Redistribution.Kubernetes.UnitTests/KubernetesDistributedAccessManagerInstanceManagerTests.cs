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
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence;
using ApplicationAccess.Redistribution.Kubernetes.Metrics;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationAccess.Redistribution.Models;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using k8s.Models;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerTests
    {
        protected String testNameSpace = "default";
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> emptyInstanceConfiguration;
        protected IDistributedAccessManagerPersistentStorageManager<TestPersistentStorageLoginCredentials> mockPersistentStorageManager;
        protected IPersistentStorageInstanceRandomNameGenerator mockPersistentStorageInstanceRandomNameGenerator;
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> mockAppSettingsConfigurer;
        protected IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> mockShardConfigurationSetPersister;
        protected IAccessManagerTemporalEventBatchReader mockSourceShardGroup1EventReader;
        protected IAccessManagerTemporalEventBatchReader mockSourceShardGroup2EventReader;
        protected IAccessManagerTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        protected IAccessManagerTemporalEventDeleter mocksourceShardGroupEventDeleter;
        protected IDistributedAccessManagerOperationRouter mockOperationRouter;
        protected IDistributedAccessManagerWriterAdministrator mockSourceShardGroup1WriterAdministrator;
        protected IDistributedAccessManagerWriterAdministrator mockSourceShardGroup2WriterAdministrator;
        protected IDistributedAccessManagerShardGroupSplitter mockShardGroupSplitter;
        protected IDistributedAccessManagerShardGroupMerger mockShardGroupMerger;
        protected IKubernetesClientShim mockKubernetesClientShim;
        protected IApplicationLogger mockApplicationLogger;
        protected IMetricLogger mockMetricLogger;
        protected Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> testShardConfigurationSetPersisterCreationFunction;
        protected Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBatchReader> testSourceShardGroupEventReaderCreationFunction;
        protected Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> testTargetShardGroupEventPersisterCreationFunction;
        protected Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventDeleter> testSourceShardGroupEventDeleterCreationFunction;
        protected Func<Uri, IDistributedAccessManagerOperationRouter> testOperationRouterCreationFunction;
        protected Func<Uri, IDistributedAccessManagerWriterAdministrator> testSourceShardGroupWriterAdministratorCreationFunction;
        protected KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers testKubernetesDistributedAccessManagerInstanceManager;

        [SetUp]
        protected void SetUp()
        {
            testNameSpace = "default";
            emptyInstanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials>();
            mockPersistentStorageManager = Substitute.For<IDistributedAccessManagerPersistentStorageManager<TestPersistentStorageLoginCredentials>>();
            mockPersistentStorageInstanceRandomNameGenerator = Substitute.For<IPersistentStorageInstanceRandomNameGenerator>();
            mockAppSettingsConfigurer = Substitute.For<IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials>>();
            mockShardConfigurationSetPersister = Substitute.For<IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>();
            mockSourceShardGroup1EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroup2EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, String, String>>();
            mocksourceShardGroupEventDeleter = Substitute.For<IAccessManagerTemporalEventDeleter>();
            mockOperationRouter = Substitute.For<IDistributedAccessManagerOperationRouter>();
            mockSourceShardGroup1WriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockSourceShardGroup2WriterAdministrator = Substitute.For<IDistributedAccessManagerWriterAdministrator>();
            mockShardGroupSplitter = Substitute.For<IDistributedAccessManagerShardGroupSplitter>();
            mockShardGroupMerger = Substitute.For<IDistributedAccessManagerShardGroupMerger>();
            mockKubernetesClientShim = Substitute.For<IKubernetesClientShim>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardConfigurationSetPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { return mockShardConfigurationSetPersister; };
            Boolean readerCreationFunctionCalled = false;
            testSourceShardGroupEventReaderCreationFunction = (TestPersistentStorageLoginCredentials credentials) => 
            { 
                if (readerCreationFunctionCalled == false)
                {
                    readerCreationFunctionCalled = true;
                    return mockSourceShardGroup1EventReader;
                }
                else
                {
                    return mockSourceShardGroup2EventReader;
                }
            };
            testTargetShardGroupEventPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { return mockTargetShardGroupEventPersister; };
            testSourceShardGroupEventDeleterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { return mocksourceShardGroupEventDeleter; };
            testOperationRouterCreationFunction = (Uri baseUrl) => { return mockOperationRouter; };
            Boolean writerAdministratorCreationFunctionCalled = false;
            testSourceShardGroupWriterAdministratorCreationFunction = (Uri baseUrl) => 
            {
                if (writerAdministratorCreationFunctionCalled == false)
                {
                    writerAdministratorCreationFunctionCalled = true;
                    return mockSourceShardGroup1WriterAdministrator;
                }
                else
                {
                    return mockSourceShardGroup2WriterAdministrator;
                }
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                emptyInstanceConfiguration, 
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator, 
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction, 
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
        }

        [Test]
        public void Constructor_StaticConfigurationParameterValidationFails()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
                (
                    CreateStaticConfiguration() with { NameSpace = null },
                    mockPersistentStorageManager,
                    mockAppSettingsConfigurer,
                    testShardConfigurationSetPersisterCreationFunction,
                    mockApplicationLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'staticConfiguration' failed to validate."));
            Assert.AreEqual("staticConfiguration", e.ParamName);
            Assert.That(e.InnerException.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration property 'NameSpace' cannot be null."));
        }

        [Test]
        public void Constructor_InstanceConfigurationParameterValidationFails()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                }
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
                (
                    CreateStaticConfiguration(),
                    instanceConfiguration,
                    mockPersistentStorageManager,
                    mockAppSettingsConfigurer,
                    testShardConfigurationSetPersisterCreationFunction,
                    mockApplicationLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'instanceConfiguration' failed to validate."));
            Assert.AreEqual("instanceConfiguration", e.ParamName);
            Assert.That(e.InnerException.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration '*ShardGroupConfiguration' properties must be either all null or all non-null."));
        }

        [Test]
        public void Constructor_InstanceConfigurationShardGroupsSorted()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> testInstanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=ApplicationAccessConfig"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        0,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    ),
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                },
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=grouptogroup_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                    )
                },
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        0,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    ),
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    )
                },
                DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/")
            };

            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                testInstanceConfiguration,
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            Assert.AreEqual(Int32.MinValue, testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(Int32.MinValue, testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
        }

        [Test]
        public void Constructor_ShardGroupConfigurationSetFieldsPopulated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> testInstanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=ApplicationAccessConfig"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        0,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    ),
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                },
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=grouptogroup_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                    )
                },
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        0,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    ),
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    )
                },
                DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/")
            };

            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                testInstanceConfiguration,
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            Assert.AreEqual(2, testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items[0].HashRangeStart);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items[1].HashRangeStart);
            Assert.AreEqual(1, testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items[0].HashRangeStart);
            Assert.AreEqual(2, testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items[0].HashRangeStart);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items[1].HashRangeStart);
        }

        [Test]
        public void DistributedOperationRouterUrl_CreateDistributedOperationRouterLoadBalancerServiceAsyncNotPreviouslyCalled()
        {
            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/");
            });

            Assert.That(e.Message, Does.StartWith($"Property 'DistributedOperationRouterUrl' cannot be set if it was not previously created by calling method CreateDistributedOperationRouterLoadBalancerServiceAsync()."));
        }

        [Test]
        public void DistributedOperationRouterUrl()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            testKubernetesDistributedAccessManagerInstanceManager.DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/");

            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.DistributedOperationRouterUrl.ToString());
        }

        [Test]
        public void Writer1Url_CreateWriter1LoadBalancerServiceAsyncNotPreviouslyCalled()
        {
            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.Writer1Url = new Uri("http://10.104.198.19:7001/");
            });

            Assert.That(e.Message, Does.StartWith($"Property 'Writer1Url' cannot be set if it was not previously created by calling method CreateWriter1LoadBalancerServiceAsync()."));
        }

        [Test]
        public void Writer1Url()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            testKubernetesDistributedAccessManagerInstanceManager.Writer1Url = new Uri("http://10.104.198.19:7001/");

            Assert.AreEqual("http://10.104.198.19:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.Writer1Url.ToString());
        }

        [Test]
        public void Writer2Url_CreateWriter2LoadBalancerServiceAsyncNotPreviouslyCalled()
        {
            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.Writer2Url = new Uri("http://10.104.198.20:7001/");
            });

            Assert.That(e.Message, Does.StartWith($"Property 'Writer2Url' cannot be set if it was not previously created by calling method CreateWriter2LoadBalancerServiceAsync()."));
        }

        [Test]
        public void Writer2Url()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            testKubernetesDistributedAccessManagerInstanceManager.Writer2Url = new Uri("http://10.104.198.20:7001/");

            Assert.AreEqual("http://10.104.198.20:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.Writer2Url.ToString());
        }

        [Test]
        public void DistributedOperationCoordinatorUrl_CreateDistributedAccessManagerInstanceAsyncNotPreviouslyCalled()
        {
            var e = Assert.Throws<InvalidOperationException>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/");
            });

            Assert.That(e.Message, Does.StartWith($"Property 'DistributedOperationCoordinatorUrl' cannot be set if it was not previously created by calling method CreateDistributedAccessManagerInstanceAsync()."));
        }

        [Test]
        public void DistributedOperationCoordinatorUrl()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            testKubernetesDistributedAccessManagerInstanceManager.DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/");

            Assert.AreEqual("http://10.104.198.19:7000/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public void CreateDistributedOperationRouterLoadBalancerServiceAsync_ServiceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:5000/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            UInt16 port = 7001;

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerServiceAsync(port);
            });

            Assert.That(e.Message, Does.StartWith($"A load balancer service for the distributed operation router has already been created."));
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerServiceAsync_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerServiceAsync_ExceptionRetrievingServiceIpAddress()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-router-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1ServiceList>(returnServices),
                Task.FromException<V1ServiceList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerServiceAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-router-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerServiceAsync(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>()); 
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation router on port 7001 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Completed creating load balancer service.");
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.DistributedOperationRouterUrl.ToString());
        }

        [Test]
        public void CreateWriter1LoadBalancerServiceAsync_ServiceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                Writer1Url = new Uri("http://10.104.198.18:7001/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            UInt16 port = 7001;

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriter1LoadBalancerServiceAsync(port);
            });

            Assert.That(e.Message, Does.StartWith($"A load balancer service for the first writer component has already been created."));
        }

        [Test]
        public async Task CreateWriter1LoadBalancerServiceAsync()
        {
            UInt16 port = 7001;
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "writer1-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateWriter1LoadBalancerServiceAsync(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Creating load balancer service 'writer1-externalservice' for writer on port 7001 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Completed creating load balancer service.");
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.Writer1Url.ToString());
        }

        [Test]
        public void CreateWriter2LoadBalancerServiceAsync_ServiceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                Writer2Url = new Uri("http://10.104.198.20:7005/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            UInt16 port = 7005;

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriter2LoadBalancerServiceAsync(port);
            });

            Assert.That(e.Message, Does.StartWith($"A load balancer service for the second writer component has already been created."));
        }

        [Test]
        public async Task CreateWriter2LoadBalancerServiceAsync()
        {
            UInt16 port = 7005;
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "writer2-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.20" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateWriter2LoadBalancerServiceAsync(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Creating load balancer service 'writer2-externalservice' for writer on port 7005 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Completed creating load balancer service.");
            Assert.AreEqual(IPAddress.Parse("10.104.198.20"), result);
            Assert.AreEqual("http://10.104.198.20:7005/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.Writer2Url.ToString());
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_DistributedOperationRouterLoadBalancerServiceNotCreated()
        {
            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"A distributed operation router load balancer service must be created via method CreateDistributedOperationRouterLoadBalancerServiceAsync() before creating a distributed AccessManager instance."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_Writer1LoadBalancerServiceNotCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"A first writer load balancer service must be created via method CreateWriter1LoadBalancerServiceAsync() before creating a distributed AccessManager instance."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_Writer2LoadBalancerServiceNotCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"), 
                Writer1Url = new Uri("http://10.104.198.20:7001/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"A second writer load balancer service must be created via method CreateWriter2LoadBalancerServiceAsync() before creating a distributed AccessManager instance."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_AccessManagerInstanceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=ApplicationAccessConfig"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                },
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=grouptogroup_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                    )
                },
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    )
                },
                DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"A distributed AccessManager instance has already been created."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_ExceptionCreatingShardConfigurationPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_shard_configuration"; 
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockPersistentStorageManager.When((storageManager) => storageManager.CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    userShardGroupConfiguration,
                    groupToGroupMappingShardGroupConfiguration,
                    groupShardGroupConfiguration
                );
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating persistent storage instance for shard configuration."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_ExceptionWritingShardConfigurationToPersistentStorage()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    userShardGroupConfiguration,
                    groupToGroupMappingShardGroupConfiguration,
                    groupShardGroupConfiguration
                );
            });

            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error writing shard configuration to persistent storage."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateDistributedAccessManagerInstanceAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials userN2147483648Credentials = new("userN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials user0Credentials = new("user0ConnectionString");
            TestPersistentStorageLoginCredentials groupToGroupN2147483648Credentials = new("groupToGroupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN2147483648Credentials = new("groupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN715827882Credentials = new("groupN715827882ConnectionString");
            TestPersistentStorageLoginCredentials group715827884Credentials = new("group715827884ConnectionString");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }, 
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-coordinator" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            ); 
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-coordinator-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(-715_827_882),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(715_827_884)
            };
            String configurationPersistentStorageInstanceName = "applicationaccesstest_shard_configuration";
            TestPersistentStorageLoginCredentials configurationCredentials = new("testConnectionString");
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_user_0").Returns(user0Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_715827884").Returns(group715827884Credentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockPersistentStorageManager.CreateAccessManagerConfigurationPersistentStorage(configurationPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(configurationCredentials);
            ShardConfigurationSet<AccessManagerRestClientConfiguration> capturedShardConfigurationSet = null;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(argumentValue => capturedShardConfigurationSet = argumentValue), true);
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
            (
                userShardGroupConfiguration,
                groupToGroupMappingShardGroupConfiguration,
                groupShardGroupConfiguration
            );

            await mockKubernetesClientShim.Received().CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockPersistentStorageManager.Received(1).CreateAccessManagerConfigurationPersistentStorage(configurationPersistentStorageInstanceName);
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(2, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(3, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, userShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, userShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", userShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", userShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupToGroupMappingShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, groupToGroupMappingShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, groupShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, groupShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, groupShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", groupShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", groupShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, groupShardGroupConfigurationSet[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, groupShardGroupConfigurationSet[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", groupShardGroupConfigurationSet[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", groupShardGroupConfigurationSet[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[11].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[11].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[11].HashRangeStart);
            Assert.AreEqual("http://group-writer-715827884-service:5000/", capturedShardConfigurationList[11].ClientConfiguration.BaseUrl.ToString());
            // Assertions on the instance configuration distributed operation coordinator URL
            Assert.AreEqual("http://10.104.198.18:7000/", instanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public async Task CreateDistributedAccessManagerInstanceAsync_ShardConfigurationPersistentStorageCredentialsAlreadyPopulated()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials userN2147483648Credentials = new("userN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials user0Credentials = new("user0ConnectionString");
            TestPersistentStorageLoginCredentials groupToGroupN2147483648Credentials = new("groupToGroupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN2147483648Credentials = new("groupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN715827882Credentials = new("groupN715827882ConnectionString");
            TestPersistentStorageLoginCredentials group715827884Credentials = new("group715827884ConnectionString");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-coordinator" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-coordinator-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(-715_827_882),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(715_827_884)
            };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> testInstanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new TestPersistentStorageLoginCredentials("alreadyPopulatedConnectionString")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                testInstanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_user_0").Returns(user0Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_715827884").Returns(group715827884Credentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            ShardConfigurationSet<AccessManagerRestClientConfiguration> capturedShardConfigurationSet = null;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(argumentValue => capturedShardConfigurationSet = argumentValue), true);
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
            (
                userShardGroupConfiguration,
                groupToGroupMappingShardGroupConfiguration,
                groupShardGroupConfiguration
            );

            await mockKubernetesClientShim.Received().CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockPersistentStorageManager.DidNotReceive().CreateAccessManagerConfigurationPersistentStorage(Arg.Any<String>());
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(2, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(3, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, userShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, userShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", userShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", userShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupToGroupMappingShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, groupToGroupMappingShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, groupShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, groupShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, groupShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", groupShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", groupShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, groupShardGroupConfigurationSet[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, groupShardGroupConfigurationSet[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", groupShardGroupConfigurationSet[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", groupShardGroupConfigurationSet[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[11].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[11].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[11].HashRangeStart);
            Assert.AreEqual("http://group-writer-715827884-service:5000/", capturedShardConfigurationList[11].ClientConfiguration.BaseUrl.ToString());
            // Assertions on the instance configuration distributed operation coordinator URL
            Assert.AreEqual("http://10.104.198.18:7000/", instanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public async Task CreateDistributedAccessManagerInstanceAsync_PersistentStorageInstanceNamePrefixBlank()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials userN2147483648Credentials = new("userN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials user0Credentials = new("user0ConnectionString");
            TestPersistentStorageLoginCredentials groupToGroupN2147483648Credentials = new("groupToGroupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN2147483648Credentials = new("groupN2147483648ConnectionString");
            TestPersistentStorageLoginCredentials groupN715827882Credentials = new("groupN715827882ConnectionString");
            TestPersistentStorageLoginCredentials group715827884Credentials = new("group715827884ConnectionString");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n715827882" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-715827884" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-coordinator" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-coordinator-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
            };
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(-715_827_882),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(715_827_884)
            };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> testInstanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new TestPersistentStorageLoginCredentials("alreadyPopulatedConnectionString")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration() with { PersistentStorageInstanceNamePrefix = "" },
                testInstanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("user_0").Returns(user0Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage("group_715827884").Returns(group715827884Credentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            ShardConfigurationSet<AccessManagerRestClientConfiguration> capturedShardConfigurationSet = null;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(argumentValue => capturedShardConfigurationSet = argumentValue), true);
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
            (
                userShardGroupConfiguration,
                groupToGroupMappingShardGroupConfiguration,
                groupShardGroupConfiguration
            );

            await mockKubernetesClientShim.Received().CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockPersistentStorageManager.DidNotReceive().CreateAccessManagerConfigurationPersistentStorage(Arg.Any<String>());
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(2, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(3, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, userShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, userShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", userShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", userShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupToGroupMappingShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, groupToGroupMappingShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", groupToGroupMappingShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, groupShardGroupConfigurationSet[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-715_827_882, groupShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, groupShardGroupConfigurationSet[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", groupShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", groupShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(715_827_884, groupShardGroupConfigurationSet[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, groupShardGroupConfigurationSet[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", groupShardGroupConfigurationSet[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", groupShardGroupConfigurationSet[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[11].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[11].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[11].HashRangeStart);
            Assert.AreEqual("http://group-writer-715827884-service:5000/", capturedShardConfigurationList[11].ClientConfiguration.BaseUrl.ToString());
            // Assertions on the instance configuration distributed operation coordinator URL
            Assert.AreEqual("http://10.104.198.18:7000/", instanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_AccessManagerInstanceDoesntExist()
        {
            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            Assert.That(e.Message, Does.StartWith($"A distributed AccessManager instance has not been created."));
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_ExceptionScalingDownShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling shard group with data element 'User' and hash range start value {Int32.MinValue}."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_ExceptionDeletingShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, "user-reader-n2147483648", "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_ExceptionDeletingDistributedOperationCoordinator()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, "operation-coordinator", "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_ExceptionDeletingDistributedOperationCoordinatorService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, "operation-coordinator-externalservice", "default").Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void DeleteDistributedAccessManagerInstanceAsync_ExceptionDeletingShardConfigurationPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockPersistentStorageManager.When((storageManager) => storageManager.DeletePersistentStorage("applicationaccesstest_shard_configuration")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            Assert.That(e.Message, Does.StartWith($"Error deleting persistent storage instance 'applicationaccesstest_shard_configuration'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task DeleteDistributedAccessManagerInstanceAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(true);

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-reader-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-writer-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-eventcache-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-reader-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-writer-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-eventcache-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-eventcache-n2147483648", "default");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_n2147483648");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_0");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_group_n2147483648");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n2147483648", "default");
            // First 6 calls are for user shard group pods, next 3 for group to group mapping pods, next 3 for group pods, and last 1 for the operation coordinator
            await mockKubernetesClientShim.Received(13).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-coordinator", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "operation-coordinator", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-coordinator-externalservice", "default");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_shard_configuration");
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceDeleted>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Scaling down and deleting shard groups...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed scaling down and deleting shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation coordinator node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation coordinator node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting shard configuration persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting shard configuration persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed AccessManager instance.");
            // Assert that the instance configuration was updated correctly
            instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.IsNull(instanceConfiguration.ShardConfigurationPersistentStorageCredentials);
            Assert.IsNull(instanceConfiguration.DistributedOperationCoordinatorUrl);
            // Assertions on the *ShardGroupConfigurationSet fields
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Count);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Count);
        }

        [Test]
        public async Task DeleteDistributedAccessManagerInstanceAsync_DeletePersistentStorageInstancesParameterFalse()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteDistributedAccessManagerInstanceAsync(false);

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-reader-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-writer-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "user-eventcache-0-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "grouptogroupmapping-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-reader-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-writer-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "group-eventcache-n2147483648-service", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-0", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-reader-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-writer-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-eventcache-0", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "grouptogroupmapping-eventcache-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-reader-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-writer-n2147483648", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-eventcache-n2147483648", "default");
            mockPersistentStorageManager.DidNotReceive().DeletePersistentStorage("applicationaccesstest_user_n2147483648");
            mockPersistentStorageManager.DidNotReceive().DeletePersistentStorage("applicationaccesstest_user_0");
            mockPersistentStorageManager.DidNotReceive().DeletePersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648");
            mockPersistentStorageManager.DidNotReceive().DeletePersistentStorage("applicationaccesstest_group_n2147483648");
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n2147483648", "default");
            // First 6 calls are for user shard group pods, next 3 for group to group mapping pods, next 3 for group pods, and last 1 for the operation coordinator
            await mockKubernetesClientShim.Received(13).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-coordinator", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "operation-coordinator", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-coordinator-externalservice", "default");
            mockPersistentStorageManager.DidNotReceive().DeletePersistentStorage("applicationaccesstest_shard_configuration");
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceDeleteTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceDeleted>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Scaling down and deleting shard groups...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed scaling down and deleting shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation coordinator node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation coordinator node.");
            mockApplicationLogger.DidNotReceive().Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting shard configuration persistent storage instance...");
            mockApplicationLogger.DidNotReceive().Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting shard configuration persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed AccessManager instance.");
            // Assert that the instance configuration was updated correctly
            instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.GroupShardGroupConfiguration.Count);
            // Assertions on the *ShardGroupConfigurationSet fields
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Count);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(0, testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Count);
        }

        [Test]
        public async Task CreateWriterLoadBalancerServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String appLabelValue = "writer1";
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerServiceAsync(appLabelValue, port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating writer load balancer service 'writer1-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerServiceAsync_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String appLabelValue = "writer1";
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerServiceAsync(appLabelValue, port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for writer load balancer service 'writer1-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerServiceAsync_ExceptionRetrievingServiceIpAddress()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String appLabelValue = "writer1";
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "writer1-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1ServiceList>(returnServices),
                Task.FromException<V1ServiceList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerServiceAsync(appLabelValue, port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for writer load balancer service 'writer1-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerServiceAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String appLabelValue = "writer1";
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "writer1-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerServiceAsync(appLabelValue, port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Creating load balancer service 'writer1-externalservice' for writer on port 7001 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Completed creating load balancer service.");
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
        }

        [Test]
        public void SplitShardGroupAsync_DistributedAccessManagerInstanceNotCreated()
        {
            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    0, 
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction, 
                    1000, 
                    5, 
                    2000,
                    mockShardGroupSplitter
                    );
            });

            Assert.That(e.Message, Does.StartWith($"A distributed AccessManager instance has not been created."));
        }

        [Test]
        public void SplitShardGroupAsync_DataElementSetToGroupToGroupMapping()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.GroupToGroupMapping, 
                    Int32.MinValue, 
                    0, 
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Shard group splitting is not supported for 'DataElement' 'GroupToGroupMapping'."));
            Assert.AreEqual("dataElement", e.ParamName);
        }

        [Test]
        public void SplitShardGroupAsync_ParameterSplitHashRangeEndLessThanParameterSplitHashRangeStart()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    0, 
                    -1,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'splitHashRangeEnd' with value -1 must be greater than or equal to parameter 'splitHashRangeStart' with value 0."));
            Assert.AreEqual("splitHashRangeEnd", e.ParamName);
        }

        [Test]
        public void SplitShardGroupAsync_HashRangeStartParameterInvalid()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    1, 
                    100, 
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'hashRangeStart' with value 1 contains an invalid hash range start value for 'User' shard groups."));
            Assert.AreEqual("hashRangeStart", e.ParamName);
        }

        [Test]
        public void SplitShardGroupAsync_SplitHashRangeStartParameterInvalid()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    Int32.MinValue, 
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'splitHashRangeStart' with value {Int32.MinValue} must be greater than parameter 'hashRangeStart' with value {Int32.MinValue}."));
            Assert.AreEqual("splitHashRangeStart", e.ParamName);


            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    0, 
                    100,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'splitHashRangeStart' with value 0 must be less than the hash range start value 0 of the next sequential shard group."));
            Assert.AreEqual("splitHashRangeStart", e.ParamName);
        }

        [Test]
        public void SplitShardGroupAsync_SplitHashRangeEndParameterInvalid()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    0, 
                    100,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'splitHashRangeEnd' with value 100 contains a different hash range end value to the hash range end value {Int32.MaxValue} of the shard group being split."));
            Assert.AreEqual("splitHashRangeEnd", e.ParamName);


            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User, 
                    Int32.MinValue, 
                    -100, 
                    -2,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'splitHashRangeEnd' with value -2 contains a different hash range end value to the hash range end value -1 of the shard group being split."));
            Assert.AreEqual("splitHashRangeEnd", e.ParamName);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingTargetShardGroupPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockPersistentStorageManager.When((storageManager) => storageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_user_0")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_user_0");
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating persistent storage instance for data element type 'User' and hash range start value 0."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingSourceShardGroupEventReader()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testSourceShardGroupEventReaderCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroupEventReader."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingTargetShardGroupEventPersister()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testTargetShardGroupEventPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct targetShardGroupEventPersister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingSourceShardGroupEventDeleter()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testSourceShardGroupEventDeleterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroupEventDeleter."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingOperationRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testOperationRouterCreationFunction = (Uri baseUrl) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct operationRouter."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCreatingSourceShardGroupWriterAdministrator()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testSourceShardGroupWriterAdministratorCreationFunction = (Uri baseUrl) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroupWriterAdministrator."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionCreatingRouterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageCreateBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid routerCreateBeginId = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(persistentStorageCreateBeginId);
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(routerCreateBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException)); 
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(routerCreateBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation router deployment."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionUpdatingWriterLoadBalancerService()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionUpdatingShardGroupConfigurationToRedirectToRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionCopyingEvents()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid eventCopyBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockMetricLogger.Begin(Arg.Any<EventCopyTime>()).Returns(eventCopyBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments)); 
            mockShardGroupSplitter.When((splitter) => splitter.CopyEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockTargetShardGroupEventPersister, 
                mockOperationRouter, 
                mockSourceShardGroup1WriterAdministrator, 
                0, 
                Int32.MaxValue, 
                false, 
                1000,
                5, 
                2000
            )).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockShardGroupSplitter.Received(1).CopyEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                0,
                Int32.MaxValue,
                false,
                1000,
                5,
                2000
            );
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(eventCopyBeginId, Arg.Any<EventCopyTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Error copying events from source shard group to target shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionCreatingNewShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns
            (
                Task.FromResult<V1Deployment>(new V1Deployment()),
                Task.FromException<V1Deployment>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(2).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating event cache node for data element type 'User' and hash range start value 0 in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionSwitchingOnRouting()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockOperationRouter.RoutingOn = Arg.Do<Boolean>((Boolean value) => { throw mockException; });

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockOperationRouter.Received(1).RoutingOn = true;
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to switch routing on."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionResumingRouterOperationsAfterEventCopy()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockOperationRouter.When((router) => router.ResumeOperations()).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockOperationRouter.Received(1).ResumeOperations();
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to resume incoming operations in the source and target shard groups."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionUpdatingShardGroupConfigurationToRedirectToTargetShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            Boolean configurationSetPersisterWriteCalled = false;
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do
            (
                (callInfo) =>
                {
                    if (configurationSetPersisterWriteCalled == false)
                    {
                        configurationSetPersisterWriteCalled = true;
                    }
                    else
                    {
                        throw mockException;
                    }
                }
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockShardConfigurationSetPersister.Received(2).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionDeletingEvents()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockShardGroupSplitter.When((splitter) => splitter.DeleteEventsFromSourceShardGroup
            (
                mocksourceShardGroupEventDeleter, 
                0, 
                Int32.MaxValue, 
                false
            )).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockShardGroupSplitter.Received(1).DeleteEventsFromSourceShardGroup(mocksourceShardGroupEventDeleter, 0, Int32.MaxValue, false);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Error deleting events from source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionPausingRouterOperations()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockOperationRouter.When((router) => router.PauseOperations()).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockOperationRouter.Received(1).PauseOperations();
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to hold/pause incoming operations in the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionRestartingSourceShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n2147483648", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n2147483648", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n2147483648", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionResumingRouterOperationsAfterEventDelete()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            Boolean receivedRouterResumeCall = false;
            mockOperationRouter.When((router) => router.ResumeOperations()).Do
            (
                (callInfo) => 
                { 
                    if (receivedRouterResumeCall == false)
                    {
                        receivedRouterResumeCall = true;
                    }
                    else
                    {
                        throw mockException;
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockOperationRouter.Received(2).ResumeOperations();
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to resume incoming operations in the source shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void SplitShardGroupAsync_ExceptionUpdatingShardGroupConfigurationToRedirectToSourceShardGroup()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            Int32 configurationSetPersisterWriteCallCount = 0;
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do
            (
                (callInfo) =>
                {
                    if (configurationSetPersisterWriteCallCount == 2)
                    {
                        throw mockException;
                    }
                    configurationSetPersisterWriteCallCount++;
                }
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            mockShardConfigurationSetPersister.Received(3).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionReversingWriterLoadBalancerServiceUpdate()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace).Returns
            (
                Task.FromResult<V1Service>(new V1Service()),
                Task.FromException<V1Service>(mockException)
            );
            
            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionDeletingOperationRouterService()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_ExceptionDeletingOperationRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testSourceShardGroupEventDeleterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupSplitter
                );
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task SplitShardGroupAsync_UserShardGroup()
        {
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageCreateBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid eventCopyBeginId = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            // This is returned whilst restarting the source shard group
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            // This is returned whilst creating the router, creating the target shard group, and restarting the source shard group
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            String persistentStorageInstanceName = "applicationaccesstest_user_0";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                CreateInstanceConfiguration("applicationaccesstest"),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(persistentStorageCreateBeginId);
            mockMetricLogger.Begin(Arg.Any<EventCopyTime>()).Returns(eventCopyBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            ShardConfigurationSet<AccessManagerRestClientConfiguration> firstCapturedShardConfigurationSet = null, secondCapturedShardConfigurationSet = null, thirdCapturedShardConfigurationSet = null;
            Int32 shardConfigurationSetPersisterWriteCallCount = 0;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>
            (
                (argumentValue) =>
                { 
                    if (shardConfigurationSetPersisterWriteCallCount == 0)
                    {
                        firstCapturedShardConfigurationSet = argumentValue;
                    }
                    else if (shardConfigurationSetPersisterWriteCallCount == 1)
                    {
                        secondCapturedShardConfigurationSet = argumentValue;
                    }
                    else
                    {
                        thirdCapturedShardConfigurationSet = argumentValue;
                    }
                    shardConfigurationSetPersisterWriteCallCount++;
                }
            ), true);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
            (
                DataElement.User,
                Int32.MinValue,
                0,
                Int32.MaxValue,
                testSourceShardGroupEventReaderCreationFunction,
                testTargetShardGroupEventPersisterCreationFunction,
                testSourceShardGroupEventDeleterCreationFunction,
                testOperationRouterCreationFunction,
                testSourceShardGroupWriterAdministratorCreationFunction,
                1000,
                5,
                2000,
                mockShardGroupSplitter
            );

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
            // Of below 4 calls, first is for the router and next 3 are for the target shard group reader, event cache, and writer
            await mockKubernetesClientShim.Received(4).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            // Of below 7 calls, first is for the router and next 3 are for the target shard group reader, event cache, and writer, and the next 3 are for the source shard group reader, event cache, and writer (as part of the source shard group restart)
            await mockKubernetesClientShim.Received(7).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockShardConfigurationSetPersister.Received(3).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockShardGroupSplitter.Received(1).CopyEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                0,
                Int32.MaxValue,
                false,
                1000,
                5,
                2000
            );
            mockOperationRouter.Received(1).RoutingOn = true;
            mockShardGroupSplitter.Received(1).DeleteEventsFromSourceShardGroup(mocksourceShardGroupEventDeleter, 0, Int32.MaxValue, false);
            mockOperationRouter.Received(1).PauseOperations();
            mockOperationRouter.Received(2).ResumeOperations();
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            // First 3 calls are made as part of the source shard group restart, the other 1 is made as part of scaling down and deleting the operation router
            await mockKubernetesClientShim.Received(4).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventCopyTime>());
            mockMetricLogger.Received(1).End(persistentStorageCreateBeginId, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<PersistentStorageInstanceCreated>());
            mockMetricLogger.Received(1).End(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupSplit>());
            mockMetricLogger.Received(1).End(eventCopyBeginId, Arg.Any<EventCopyTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedOperationRouterNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, $"Splitting User shard group with hash range start value {Int32.MinValue} to new shard group at hash range start value 0...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'User' and hash range start value 0...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating writer load balancer service to target source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed updating writer load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to router...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to target shard group...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Pausing operations in the source shard group.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Resuming operations in the source shard group.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to source shard group...");
            mockApplicationLogger.Received(3).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed updating shard group configuration.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Reversing update to writer load balancer service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed reversing update to writer load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed splitting shard group.");
            // Assertions on the first updated shard configuration
            //   Fine grained tests on methods CreateShardConfigurationSet() and ShardConfigurationSetPersister.Write() are already performed in tests CreateDistributedAccessManagerInstanceAsync() and UpdateAndPersistShardConfigurationSets()
            //   Hence will just perform cursory checks here
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> firstCapturedShardConfigurationList = new(firstCapturedShardConfigurationSet.Items);
            Assert.AreEqual(8, firstCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, firstCapturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, firstCapturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> secondCapturedShardConfigurationList = new(secondCapturedShardConfigurationSet.Items);
            Assert.AreEqual(8, secondCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", secondCapturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", secondCapturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, secondCapturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", secondCapturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, secondCapturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", secondCapturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> thirdCapturedShardConfigurationList = new(thirdCapturedShardConfigurationSet.Items);
            Assert.AreEqual(8, thirdCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, thirdCapturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, thirdCapturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, thirdCapturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", thirdCapturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, thirdCapturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, thirdCapturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, thirdCapturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", thirdCapturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, thirdCapturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, thirdCapturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, thirdCapturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", thirdCapturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, thirdCapturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, thirdCapturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, thirdCapturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", thirdCapturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            // Assert that the instance configuration was updated correctly (including sorting)
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupShardGroupConfiguration.Count);
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = instanceConfiguration.UserShardGroupConfiguration;
            Assert.AreEqual(Int32.MinValue, userShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648", userShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(storageCredentials.ConnectionString, userShardGroupConfiguration[1].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-0-service:5000/", userShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", userShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(2, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648", userShardGroupConfigurationSet[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(storageCredentials.ConnectionString, userShardGroupConfigurationSet[1].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-0-service:5000/", userShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", userShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
        }

        [Test]
        public async Task SplitShardGroupAsync_GroupShardGroup()
        {
            Guid shardGroupSplitBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageCreateBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid eventCopyBeginId = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            // This is returned whilst restarting the source shard group
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            // This is returned whilst creating the router, creating the target shard group, and restarting the source shard group
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n1073741824" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n1073741824" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n1073741824" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            String persistentStorageInstanceName = "group_n1073741824";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with 
            {
                DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50,
                PersistentStorageInstanceNamePrefix = ""
            };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            instanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-0-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupSplitTime>()).Returns(shardGroupSplitBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(persistentStorageCreateBeginId);
            mockMetricLogger.Begin(Arg.Any<EventCopyTime>()).Returns(eventCopyBeginId);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            ShardConfigurationSet<AccessManagerRestClientConfiguration> firstCapturedShardConfigurationSet = null, secondCapturedShardConfigurationSet = null, thirdCapturedShardConfigurationSet = null;
            Int32 shardConfigurationSetPersisterWriteCallCount = 0;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>
            (
                (argumentValue) =>
                {
                    if (shardConfigurationSetPersisterWriteCallCount == 0)
                    {
                        firstCapturedShardConfigurationSet = argumentValue;
                    }
                    else if (shardConfigurationSetPersisterWriteCallCount == 1)
                    {
                        secondCapturedShardConfigurationSet = argumentValue;
                    }
                    else
                    {
                        thirdCapturedShardConfigurationSet = argumentValue;
                    }
                    shardConfigurationSetPersisterWriteCallCount++;
                }
            ), true);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.SplitShardGroupAsync
            (
                DataElement.Group,
                Int32.MinValue,
                -1073741824,
                -1,
                testSourceShardGroupEventReaderCreationFunction,
                testTargetShardGroupEventPersisterCreationFunction,
                testSourceShardGroupEventDeleterCreationFunction,
                testOperationRouterCreationFunction,
                testSourceShardGroupWriterAdministratorCreationFunction,
                1000,
                5,
                2000,
                mockShardGroupSplitter
            );

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
            // Of below 4 calls, first is for the router and next 3 are for the target shard group reader, event cache, and writer
            await mockKubernetesClientShim.Received(4).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            // Of below 7 calls, first is for the router and next 3 are for the target shard group reader, event cache, and writer, and the next 3 are for the source shard group reader, event cache, and writer (as part of the source shard group restart)
            await mockKubernetesClientShim.Received(7).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockShardConfigurationSetPersister.Received(3).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockShardGroupSplitter.Received(1).CopyEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                -1073741824,
                -1,
                true,
                1000,
                5,
                2000
            );
            mockOperationRouter.Received(1).RoutingOn = true;
            mockShardGroupSplitter.Received(1).DeleteEventsFromSourceShardGroup(mocksourceShardGroupEventDeleter, -1073741824, -1, true);
            mockOperationRouter.Received(1).PauseOperations();
            mockOperationRouter.Received(2).ResumeOperations();
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            // First 3 calls are made as part of the source shard group restart, the other 1 is made as part of scaling down and deleting the operation router
            await mockKubernetesClientShim.Received(4).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventCopyTime>());
            mockMetricLogger.Received(1).End(persistentStorageCreateBeginId, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<PersistentStorageInstanceCreated>());
            mockMetricLogger.Received(1).End(shardGroupSplitBeginId, Arg.Any<ShardGroupSplitTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupSplit>());
            mockMetricLogger.Received(1).End(eventCopyBeginId, Arg.Any<EventCopyTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedOperationRouterNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, $"Splitting Group shard group with hash range start value {Int32.MinValue} to new shard group at hash range start value -1073741824...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'Group' and hash range start value -1073741824...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating writer load balancer service to target source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed updating writer load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to router...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to target shard group...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Pausing operations in the source shard group.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Resuming operations in the source shard group.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to source shard group...");
            mockApplicationLogger.Received(3).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed updating shard group configuration.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Reversing update to writer load balancer service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed reversing update to writer load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed splitting shard group.");
            // Assertions on the first updated shard configuration
            //   Fine grained tests on methods CreateShardConfigurationSet() and ShardConfigurationSetPersister.Write() are already performed in tests CreateDistributedAccessManagerInstanceAsync() and UpdateAndPersistShardConfigurationSets()
            //   Hence will just perform cursory checks here
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> firstCapturedShardConfigurationList = new(firstCapturedShardConfigurationSet.Items);
            Assert.AreEqual(10, firstCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(-1073741824, firstCapturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(-1073741824, firstCapturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(0, firstCapturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-0-service:5000/", firstCapturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(0, firstCapturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-0-service:5000/", firstCapturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> secondCapturedShardConfigurationList = new(secondCapturedShardConfigurationSet.Items);
            Assert.AreEqual(10, secondCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", secondCapturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", secondCapturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(-1073741824, secondCapturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n1073741824-service:5000/", secondCapturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(-1073741824, secondCapturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n1073741824-service:5000/", secondCapturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(0, secondCapturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-0-service:5000/", secondCapturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(0, secondCapturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-0-service:5000/", secondCapturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> thirdCapturedShardConfigurationList = new(thirdCapturedShardConfigurationSet.Items);
            Assert.AreEqual(10, thirdCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, thirdCapturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, thirdCapturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", thirdCapturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, thirdCapturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, thirdCapturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", thirdCapturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, thirdCapturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(-1073741824, thirdCapturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n1073741824-service:5000/", thirdCapturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, thirdCapturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(-1073741824, thirdCapturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n1073741824-service:5000/", thirdCapturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, thirdCapturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(0, thirdCapturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-0-service:5000/", thirdCapturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, thirdCapturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, thirdCapturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(0, thirdCapturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-0-service:5000/", thirdCapturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            // Assert that the instance configuration was updated correctly (including sorting)
            instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = instanceConfiguration.GroupShardGroupConfiguration;
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648", groupShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-1073741824, groupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(storageCredentials.ConnectionString, groupShardGroupConfiguration[1].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n1073741824-service:5000/", groupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n1073741824-service:5000/", groupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, groupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_0", groupShardGroupConfiguration[2].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-0-service:5000/", groupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-0-service:5000/", groupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(1, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(3, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648", groupShardGroupConfigurationSet[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(-1073741824, groupShardGroupConfigurationSet[1].HashRangeStart);
            Assert.AreEqual(storageCredentials.ConnectionString, groupShardGroupConfigurationSet[1].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n1073741824-service:5000/", groupShardGroupConfigurationSet[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n1073741824-service:5000/", groupShardGroupConfigurationSet[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, groupShardGroupConfigurationSet[2].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_0", groupShardGroupConfigurationSet[2].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-0-service:5000/", groupShardGroupConfigurationSet[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-0-service:5000/", groupShardGroupConfigurationSet[2].WriterNodeClientConfiguration.BaseUrl.ToString());
        }

        [Test]
        public void MergeShardGroupsAsync_DistributedAccessManagerInstanceNotCreated()
        {
            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                    );
            });

            Assert.That(e.Message, Does.StartWith($"A distributed AccessManager instance has not been created."));
        }

        [Test]
        public void MergeShardGroupsAsync_DataElementSetToGroupToGroupMapping()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.GroupToGroupMapping,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Shard group merging is not supported for 'DataElement' 'GroupToGroupMapping'."));
            Assert.AreEqual("dataElement", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_ParameterSourceShardGroup2HashRangeEndLessThanParameterSourceShardGroup2HashRangeStart()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    -1,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup2HashRangeEnd' with value -1 must be greater than or equal to parameter 'sourceShardGroup2HashRangeStart' with value 0."));
            Assert.AreEqual("sourceShardGroup2HashRangeEnd", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_SourceShardGroup1HashRangeStartParameterInvalid()
        {
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    1,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup1HashRangeStart' with value 1 contains an invalid hash range start value for 'User' shard groups."));
            Assert.AreEqual("sourceShardGroup1HashRangeStart", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_SourceShardGroup2HashRangeStartParameterEqualToSourceShardGroup1HashRangeStartParameter()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    Int32.MinValue,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup2HashRangeStart' with value {Int32.MinValue} must be greater than parameter 'sourceShardGroup1HashRangeStart' with value {Int32.MinValue}."));
            Assert.AreEqual("sourceShardGroup2HashRangeStart", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_SourceShardGroup2HashRangeStartParameterInvalid()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup2HashRangeStart' with value 0 contains an invalid hash range start value for 'User' shard groups."));
            Assert.AreEqual("sourceShardGroup2HashRangeStart", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_ParameterSourceShardGroup1HashRangeStartAndSourceShardGroup2HashRangeStartDoNotContainConsecutiveShardGroups()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    -1073741824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n1073741824-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n1073741824-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"The next consecutive shard group after shard group with hash range start value -2147483648 has hash range start value -1073741824, whereas parameter 'sourceShardGroup2HashRangeStart' contained 0.  The shard groups specified by parameters 'sourceShardGroup1HashRangeStart' and 'sourceShardGroup2HashRangeStart' must be consecutive."));
            Assert.AreEqual("sourceShardGroup2HashRangeStart", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_SourceShardGroup2HashRangeEndParameterInvalid()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("applicationaccesstest");
            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    -1073741824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n1073741824-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n1073741824-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    -1073741824,
                    0,
                    100,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup2HashRangeEnd' with value 100 contains a different hash range end value to the hash range end value {Int32.MaxValue} of the second source shard group being merged."));
            Assert.AreEqual("sourceShardGroup2HashRangeEnd", e.ParamName);


            e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    -1073741824,
                    -2,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardGroup2HashRangeEnd' with value -2 contains a different hash range end value to the hash range end value -1 of the second shard group being merged."));
            Assert.AreEqual("sourceShardGroup2HashRangeEnd", e.ParamName);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingTemporaryPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.When((storageManager) => storageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_kwllkgqulnfb")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_kwllkgqulnfb");
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating temporary persistent storage instance."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingSourceShardGroup1EventReader()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testSourceShardGroupEventReaderCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroup1EventReader."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingSourceShardGroup2EventReader()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean creationFunctionCalled = false;
            testSourceShardGroupEventReaderCreationFunction = (TestPersistentStorageLoginCredentials credentials) => 
            { 
                if (creationFunctionCalled == false)
                {
                    creationFunctionCalled = true;
                    return mockSourceShardGroup1EventReader;
                }
                else
                {
                    throw mockException;
                }
            };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroup2EventReader."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingTargetShardGroupEventPersister()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testTargetShardGroupEventPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct targetShardGroupEventPersister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingOperationRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testOperationRouterCreationFunction = (Uri baseUrl) => { throw mockException; };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct operationRouter."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingSourceShardGroup1WriterAdministrator()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testSourceShardGroupWriterAdministratorCreationFunction = (Uri baseUrl) => { throw mockException; };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroup1WriterAdministrator."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionCreatingSourceShardGroup2WriterAdministrator()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Boolean creationFunctionCalled = false;
            testSourceShardGroupWriterAdministratorCreationFunction = (Uri baseUrl) => 
            {
                if (creationFunctionCalled == false)
                {
                    creationFunctionCalled = true;
                    return mockSourceShardGroup1WriterAdministrator;
                }
                else
                {
                    throw mockException;
                }
            };
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(testBeginId1);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to construct sourceShardGroup2WriterAdministrator."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionCreatingRouterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid routerCreateBeginId = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(routerCreateBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(routerCreateBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation router deployment."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionUpdatingWriter1LoadBalancerService()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionUpdatingWriter2LoadBalancerService()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionUpdatingShardGroupConfigurationToRedirectToRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionMergingEvents()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid eventMergeBeginId = Guid.Parse("22908a07-eaac-42e4-ad1d-6794a835e3bd");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<EventMergeTime>()).Returns(eventMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockShardGroupMerger.When((merger) => merger.MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister, 
                mockOperationRouter, 
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                1000,
                5,
                2000
            )).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(eventMergeBeginId, Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>()); 
            Assert.That(e.Message, Does.StartWith($"Error merging events from source shard group to target shard group."));
            Assert.AreSame(mockException, e.InnerException);
        }


        [Test]
        public void MergeShardGroupsAsync_ExceptionScalingDownSourceShardGroup1()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling down source shard group 1 with data element 'User' and hash range start value {Int32.MinValue}."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionRenamingSourceShardGroup1PersistentStorage()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageInstanceRenameBeginId = Guid.Parse("22908a07-eaac-42e4-ad1d-6794a835e3bd");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceRenameTime>()).Returns(persistentStorageInstanceRenameBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockPersistentStorageManager.When((manager) => manager.RenamePersistentStorage("applicationaccesstest_user_n2147483648", "applicationaccesstest_user_n2147483648_old")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).CancelBegin(persistentStorageInstanceRenameBeginId, Arg.Any<PersistentStorageInstanceRenameTime>());
            Assert.That(e.Message, Does.StartWith($"Error renaming source shard group 1 persistent storage instance."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionRenamingTemporaryPersistentStorage()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageInstanceRenameBeginId1 = Guid.Parse("22908a07-eaac-42e4-ad1d-6794a835e3bd");
            Guid persistentStorageInstanceRenameBeginId2 = Guid.Parse("044c68d4-990d-4d53-939f-a0d7d7456eea");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceRenameTime>()).Returns(persistentStorageInstanceRenameBeginId1, persistentStorageInstanceRenameBeginId2);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockPersistentStorageManager.When((manager) => manager.RenamePersistentStorage("applicationaccesstest_kwllkgqulnfb", "applicationaccesstest_user_n2147483648")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(2).Begin(Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).CancelBegin(persistentStorageInstanceRenameBeginId2, Arg.Any<PersistentStorageInstanceRenameTime>());
            Assert.That(e.Message, Does.StartWith($"Error renaming temporary persistent storage instance."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionScalingUpSourceShardGroup1()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1DeploymentList>(returnDeployments),
                Task.FromException<V1DeploymentList>(mockException)
            );
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling up source shard group 1 with data element 'User' and hash range start value -2147483648."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionSwitchingOffRouting()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockOperationRouter.RoutingOn = Arg.Do<Boolean>((Boolean value) => { throw mockException; });

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to switch routing off."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionResumingRouterOperations()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockOperationRouter.When((router) => router.ResumeOperations()).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to resume incoming operations in the source and target shard groups."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionUpdatingShardGroupConfigurationToRedirectToSourceShardGroup1()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            Int32 configurationSetPersisterWriteCallCount = 0;
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do
            (
                (callInfo) =>
                {
                    if (configurationSetPersisterWriteCallCount == 1)
                    {
                        throw mockException;
                    }
                    configurationSetPersisterWriteCallCount++;
                }
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockShardConfigurationSetPersister.Received(2).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionScalingDownSourceShardGroup2()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1PodList>(returnPods),
                Task.FromResult<V1PodList>(returnPods),
                Task.FromResult<V1PodList>(returnPods),
                Task.FromException<V1PodList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling down source shard group 2 with data element 'User' and hash range start value 0."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionDeletingSourceShardGroup2()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String tempPersistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={tempPersistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(tempPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockPersistentStorageManager.When((storageManager) => storageManager.DeletePersistentStorage("applicationaccesstest_user_0")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_0");
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeShardGroupsAsync_ExceptionDeletingSourceShardGroup1OriginalPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String tempPersistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={tempPersistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(tempPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockPersistentStorageManager.When((storageManager) => storageManager.DeletePersistentStorage("applicationaccesstest_user_n2147483648_old")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_n2147483648_old");
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionReversingWriterLoadBalancerServicesUpdate()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String tempPersistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={tempPersistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(tempPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace).Returns
            (
                Task.FromResult<V1Service>(new V1Service()),
                Task.FromException<V1Service>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionDeletingOperationRouterService()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String tempPersistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={tempPersistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(tempPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_ExceptionDeletingOperationRouter()
        {
            var mockException = new Exception("Mock exception");
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String tempPersistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={tempPersistentStorageInstanceName}");
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(tempPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
                (
                    DataElement.User,
                    Int32.MinValue,
                    0,
                    Int32.MaxValue,
                    testSourceShardGroupEventReaderCreationFunction,
                    testTargetShardGroupEventPersisterCreationFunction,
                    testOperationRouterCreationFunction,
                    testSourceShardGroupWriterAdministratorCreationFunction,
                    1000,
                    5,
                    2000,
                    mockShardGroupMerger
                );
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).CancelBegin(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task MergeShardGroupsAsync_UserShardGroups()
        {
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageCreateBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid persistentStorageInstanceRenameBeginId1 = Guid.Parse("22908a07-eaac-42e4-ad1d-6794a835e3bd");
            Guid persistentStorageInstanceRenameBeginId2 = Guid.Parse("044c68d4-990d-4d53-939f-a0d7d7456eea");
            Guid eventMergeBeginId = Guid.Parse("c220cc98-3f1d-4c36-8051-cd540c41b7d7");
            String persistentStorageInstanceName = "applicationaccesstest_kwllkgqulnfb";
            // This is returned whilst shutting down the first source shard group
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            // This is returned both whilst creating router and whilst restarting the first source shard group
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50 };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateTwoUserShardGroupInstanceConfiguration("applicationaccesstest");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(persistentStorageCreateBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceRenameTime>()).Returns(persistentStorageInstanceRenameBeginId1, persistentStorageInstanceRenameBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventMergeTime>()).Returns(eventMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            List<ShardConfigurationSet<AccessManagerRestClientConfiguration>> capturedShardConfigurationSets = new();
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>
            (
                (argumentValue) => { capturedShardConfigurationSets.Add(argumentValue); }
            ), true);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
            (
                DataElement.User,
                Int32.MinValue,
                0,
                Int32.MaxValue,
                testSourceShardGroupEventReaderCreationFunction,
                testTargetShardGroupEventPersisterCreationFunction,
                testOperationRouterCreationFunction,
                testSourceShardGroupWriterAdministratorCreationFunction,
                1000,
                5,
                2000,
                mockShardGroupMerger
            );

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            // Of below 4 calls, first is for the router and next 3 are for source shard group 1 reader, event cache, and writer (as part of the source shard group 1 restart)
            await mockKubernetesClientShim.Received(4).ListNamespacedDeploymentAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace);
            // First 3 calls are made as part of the first source shard group shut down, second 3 the second source shard group shut down, and other 1 scaling down and deleting the operation router
            await mockKubernetesClientShim.Received(7).ListNamespacedPodAsync(null, testNameSpace);
            mockShardConfigurationSetPersister.Received(2).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockShardGroupMerger.Received(1).MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                1000,
                5,
                2000
            );
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n2147483648", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n2147483648", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n2147483648", testNameSpace);
            mockPersistentStorageManager.Received(1).RenamePersistentStorage("applicationaccesstest_user_n2147483648", "applicationaccesstest_user_n2147483648_old");
            mockPersistentStorageManager.Received(1).RenamePersistentStorage("applicationaccesstest_kwllkgqulnfb", "applicationaccesstest_user_n2147483648");
            mockOperationRouter.Received(1).RoutingOn = false;
            mockOperationRouter.Received(1).ResumeOperations();
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "user-eventcache-0", "default");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_0");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("applicationaccesstest_user_n2147483648_old"); 
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(2).Begin(Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).End(persistentStorageCreateBeginId, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<PersistentStorageInstanceCreated>());
            mockMetricLogger.Received(1).End(persistentStorageInstanceRenameBeginId1, Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<PersistentStorageInstanceRenamed>());
            mockMetricLogger.Received(1).End(persistentStorageInstanceRenameBeginId2, Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).End(eventMergeBeginId, Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).End(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupsMerged>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Merging User shard group with hash range start value {Int32.MinValue} with shard group with hash range start value 0...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Creating temporary persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed creating temporary persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating writer 1 load balancer service to target first source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating writer 1 load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating writer 2 load balancer service to target first source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating writer 2 load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to router...");
            mockApplicationLogger.Received(2).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating shard group configuration.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Renaming source shard group 1 persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed renaming source shard group 1 persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Renaming temporary persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed renaming temporary persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to source shard group 1...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting original source shard group 1 persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Reversing updates to writer load balancer services...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed reversing updates to writer load balancer services.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed merging shard groups.");
            Assert.AreEqual(2, capturedShardConfigurationSets.Count);
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> firstCapturedShardConfigurationList = new(capturedShardConfigurationSets[0].Items);
            Assert.AreEqual(6, firstCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, firstCapturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.GroupToGroupMapping, firstCapturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://grouptogroup-reader-n2147483648-service:5000/", firstCapturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> secondCapturedShardConfigurationList = new(capturedShardConfigurationSets[1].Items);
            Assert.AreEqual(6, secondCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", secondCapturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, secondCapturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", secondCapturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            // Assert that the instance configuration was updated correctly (including sorting)
            instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupShardGroupConfiguration.Count);
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = instanceConfiguration.UserShardGroupConfiguration;
            Assert.AreEqual(Int32.MinValue, userShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648", userShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(1, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccesstest_user_n2147483648", userShardGroupConfigurationSet[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", userShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", userShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
        }

        [Test]
        public async Task MergeShardGroupsAsync_GroupShardGroups()
        {
            Guid shardGroupMergeBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid persistentStorageCreateBeginId = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid persistentStorageInstanceRenameBeginId1 = Guid.Parse("22908a07-eaac-42e4-ad1d-6794a835e3bd");
            Guid persistentStorageInstanceRenameBeginId2 = Guid.Parse("044c68d4-990d-4d53-939f-a0d7d7456eea");
            Guid eventMergeBeginId = Guid.Parse("c220cc98-3f1d-4c36-8051-cd540c41b7d7");
            String persistentStorageInstanceName = "kwllkgqulnfb";
            // This is returned whilst shutting down the first source shard group
            V1PodList returnPods = new
            (
                new List<V1Pod>()
            );
            // This is returned both whilst creating router and whilst restarting the first source shard group
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-n100" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-reader-n100" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-n100" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with 
            { 
                DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 50, 
                PersistentStorageInstanceNamePrefix = ""
            };
            staticConfiguration.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate["ShardConfigurationRefresh"]["RefreshInterval"] = 100;
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            instanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    -100,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n100"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n100-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n100-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupMergeTime>()).Returns(shardGroupMergeBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(persistentStorageCreateBeginId);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceRenameTime>()).Returns(persistentStorageInstanceRenameBeginId1, persistentStorageInstanceRenameBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventMergeTime>()).Returns(eventMergeBeginId);
            mockPersistentStorageInstanceRandomNameGenerator.Generate().Returns<String>("kwllkgqulnfb");
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            List<ShardConfigurationSet<AccessManagerRestClientConfiguration>> capturedShardConfigurationSets = new();
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>
            (
                (argumentValue) => { capturedShardConfigurationSets.Add(argumentValue); }
            ), true);
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.MergeShardGroupsAsync
            (
                DataElement.Group,
                Int32.MinValue,
                -100,
                Int32.MaxValue,
                testSourceShardGroupEventReaderCreationFunction,
                testTargetShardGroupEventPersisterCreationFunction,
                testOperationRouterCreationFunction,
                testSourceShardGroupWriterAdministratorCreationFunction,
                1000,
                5,
                2000,
                mockShardGroupMerger
            );

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage(persistentStorageInstanceName);
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            // Of below 4 calls, first is for the router and next 3 are for source shard group 1 reader, event cache, and writer (as part of the source shard group 1 restart)
            await mockKubernetesClientShim.Received(4).ListNamespacedDeploymentAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer1-externalservice", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), "writer2-externalservice", testNameSpace);
            // First 3 calls are made as part of the first source shard group shut down, second 3 the second source shard group shut down, and other 1 scaling down and deleting the operation router
            await mockKubernetesClientShim.Received(7).ListNamespacedPodAsync(null, testNameSpace);
            mockShardConfigurationSetPersister.Received(2).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockShardGroupMerger.Received(1).MergeEventsToTargetShardGroup
            (
                mockSourceShardGroup1EventReader,
                mockSourceShardGroup2EventReader,
                mockTargetShardGroupEventPersister,
                mockOperationRouter,
                mockSourceShardGroup1WriterAdministrator,
                mockSourceShardGroup2WriterAdministrator,
                1000,
                5,
                2000
            );
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-eventcache-n2147483648", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-writer-n2147483648", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-reader-n2147483648", testNameSpace);
            mockPersistentStorageManager.Received(1).RenamePersistentStorage("group_n2147483648", "group_n2147483648_old");
            mockPersistentStorageManager.Received(1).RenamePersistentStorage("kwllkgqulnfb", "group_n2147483648");
            mockOperationRouter.Received(1).RoutingOn = false;
            mockOperationRouter.Received(1).ResumeOperations();
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-eventcache-n100", "default");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("group_n100");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage("group_n2147483648_old");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, "operation-router-service", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "operation-router", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(2).Begin(Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).End(persistentStorageCreateBeginId, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).End(eventMergeBeginId, Arg.Any<EventMergeTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<PersistentStorageInstanceCreated>());
            mockMetricLogger.Received(1).End(persistentStorageInstanceRenameBeginId1, Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<PersistentStorageInstanceRenamed>());
            mockMetricLogger.Received(1).End(persistentStorageInstanceRenameBeginId2, Arg.Any<PersistentStorageInstanceRenameTime>());
            mockMetricLogger.Received(1).End(shardGroupMergeBeginId, Arg.Any<ShardGroupMergeTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupsMerged>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Merging Group shard group with hash range start value {Int32.MinValue} with shard group with hash range start value -100...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Creating temporary persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed creating temporary persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating writer 1 load balancer service to target first source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating writer 1 load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating writer 2 load balancer service to target first source shard group writer node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating writer 2 load balancer service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to router...");
            mockApplicationLogger.Received(2).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed updating shard group configuration.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Renaming source shard group 1 persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed renaming source shard group 1 persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Renaming temporary persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed renaming temporary persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Resuming operations in the source and target shard groups.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Updating shard group configuration to redirect to source shard group 1...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting original source shard group 1 persistent storage instance...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Reversing updates to writer load balancer services...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed reversing updates to writer load balancer services.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node cluster ip service...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node cluster ip service.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting distributed operation router node.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed merging shard groups.");
            Assert.AreEqual(2, capturedShardConfigurationSets.Count);
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> firstCapturedShardConfigurationList = new(capturedShardConfigurationSets[0].Items);
            Assert.AreEqual(6, firstCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.GroupToGroupMapping, firstCapturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://grouptogroup-writer-n2147483648-service:5000/", firstCapturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, firstCapturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, firstCapturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, firstCapturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, firstCapturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://operation-router-service:5000/", firstCapturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> secondCapturedShardConfigurationList = new(capturedShardConfigurationSets[1].Items);
            Assert.AreEqual(6, secondCapturedShardConfigurationList.Count);
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, secondCapturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", secondCapturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.Group, secondCapturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, secondCapturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, secondCapturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", secondCapturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            // Assert that the instance configuration was updated correctly (including sorting)
            instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupShardGroupConfiguration.Count);
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfiguration = instanceConfiguration.GroupShardGroupConfiguration;
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648", groupShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the *ShardGroupConfigurationSet fields
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupToGroupMappingShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupToGroupMappingShardGroupConfigurationSet.Items;
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> groupShardGroupConfigurationSet = testKubernetesDistributedAccessManagerInstanceManager.GroupShardGroupConfigurationSet.Items;
            Assert.AreEqual(1, userShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupToGroupMappingShardGroupConfigurationSet.Count);
            Assert.AreEqual(1, groupShardGroupConfigurationSet.Count);
            Assert.AreEqual(Int32.MinValue, groupShardGroupConfigurationSet[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_n2147483648", groupShardGroupConfigurationSet[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", groupShardGroupConfigurationSet[0].WriterNodeClientConfiguration.BaseUrl.ToString());
        }

        [Test]
        public void UpdateAndPersistShardConfigurationSets_ExceptionCreatingShardConfigurationSetPersister()
        {
            var mockException = new Exception("Mock exception");
            testShardConfigurationSetPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.UpdateAndPersistShardConfigurationSets
                (
                    DataElement.User,
                    new List<HashRangeStartAndClientConfigurations>(),
                    new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<Int32>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to construct shardConfigurationSetPersister."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void UpdateAndPersistShardConfigurationSets_ExceptionUpdatingShardConfigurationInPersistentStorage()
        {
            var mockException = new Exception("Mock exception");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                CreateInstanceConfiguration(""),
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockShardConfigurationSetPersister.When((persister) => persister.Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.UpdateAndPersistShardConfigurationSets
                (
                    DataElement.User, 
                    new List<HashRangeStartAndClientConfigurations>(), 
                    new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(), 
                    new List<Int32>()
                );
            });

            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            Assert.That(e.Message, Does.StartWith($"Error updating shard configuration in persistent storage."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void UpdateAndPersistShardConfigurationSets()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = CreateInstanceConfiguration("");
            instanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    1_073_741_824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-1073741824-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-1073741824-service:5000/"))
                )
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            Uri testUri1 = new("http://127.0.0.1/");
            Uri testUri2 = new("http://127.0.0.2/");
            Uri testUri3 = new("http://127.0.0.3/");
            Uri testUri4 = new("http://127.0.0.4/");
            List<HashRangeStartAndClientConfigurations> testConfigurationUpdates = new()
            {
                new HashRangeStartAndClientConfigurations
                {
                    HashRangeStart = Int32.MinValue,
                    ReaderNodeClientConfiguration = new AccessManagerRestClientConfiguration(testUri1),
                    WriterNodeClientConfiguration = new AccessManagerRestClientConfiguration(testUri2)
                },
                new HashRangeStartAndClientConfigurations
                {
                    HashRangeStart = 0,
                    ReaderNodeClientConfiguration = new AccessManagerRestClientConfiguration(testUri3),
                    WriterNodeClientConfiguration = new AccessManagerRestClientConfiguration(testUri4)
                }
            };
            List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> testConfigurationAdditions = new()
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    -1_073_741_824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n1073741824"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n1073741824-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n1073741824-service:5000/"))
                )
            };
            List<Int32> testConfigurationDeletes = new() { 1_073_741_824 };
            ShardConfigurationSet<AccessManagerRestClientConfiguration> capturedShardConfigurationSet = null;
            mockShardConfigurationSetPersister.Write(Arg.Do<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(argumentValue => capturedShardConfigurationSet = argumentValue), true);

            testKubernetesDistributedAccessManagerInstanceManager.UpdateAndPersistShardConfigurationSets
            (
                DataElement.User, 
                testConfigurationUpdates,
                testConfigurationAdditions,
                testConfigurationDeletes
            );

            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            // Assert contents of the returned shard group configuration
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> userShardGroupConfiguration = testKubernetesDistributedAccessManagerInstanceManager.UserShardGroupConfigurationSet.Items;
            Assert.AreEqual(3, userShardGroupConfiguration.Count);
            Assert.AreEqual(Int32.MinValue, userShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648", userShardGroupConfiguration[0].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(testUri1, userShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl);
            Assert.AreEqual(testUri2, userShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl);
            Assert.AreEqual(-1_073_741_824, userShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n1073741824", userShardGroupConfiguration[1].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual("http://user-reader-n1073741824-service:5000/", userShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n1073741824-service:5000/", userShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(0, userShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_0", userShardGroupConfiguration[2].PersistentStorageCredentials.ConnectionString);
            Assert.AreEqual(testUri3, userShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl);
            Assert.AreEqual(testUri4, userShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl);
            // Assert the persisted shard configuration
            //   Fine grained tests on methods CreateShardConfigurationSet() and ShardConfigurationSetPersister.Write() are already performed in test CreateDistributedAccessManagerInstanceAsync()
            //   Hence will just perform cursory checks here
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(-1_073_741_824, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-n1073741824-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(-1_073_741_824, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-n1073741824-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
        }

        [Test]
        public void GetDistributedOperationCoordinatorConfigurationRefreshInterval()
        {
            Int32 result = testKubernetesDistributedAccessManagerInstanceManager.GetDistributedOperationCoordinatorConfigurationRefreshInterval();

            Assert.AreEqual(11000, result);
        }

        [Test]
        public void GenerateNodeIdentifier()
        {
            String result = testKubernetesDistributedAccessManagerInstanceManager.GenerateNodeIdentifier(DataElement.User, NodeType.Reader, -2147483648);

            Assert.AreEqual("user-reader-n2147483648", result);


            result = testKubernetesDistributedAccessManagerInstanceManager.GenerateNodeIdentifier(DataElement.Group, NodeType.EventCache, -1);

            Assert.AreEqual("group-eventcache-n1", result);


            result = testKubernetesDistributedAccessManagerInstanceManager.GenerateNodeIdentifier(DataElement.GroupToGroupMapping, NodeType.Writer, 0);

            Assert.AreEqual("grouptogroupmapping-writer-0", result);


            result = testKubernetesDistributedAccessManagerInstanceManager.GenerateNodeIdentifier(DataElement.User, NodeType.Reader, 1);

            Assert.AreEqual("user-reader-1", result);
        }

        [Test]
        public void GeneratePersistentStorageInstanceName()
        {
            String result = testKubernetesDistributedAccessManagerInstanceManager.GeneratePersistentStorageInstanceName(DataElement.User, -100);

            Assert.AreEqual("applicationaccesstest_user_n100", result);


            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { PersistentStorageInstanceNamePrefix = "" };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                emptyInstanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            result = testKubernetesDistributedAccessManagerInstanceManager.GeneratePersistentStorageInstanceName(DataElement.User, -100);

            Assert.AreEqual("user_n100", result);
        }

        [Test]
        public void GetLoadBalancerServiceScheme()
        {
            String result = testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceScheme();

            Assert.AreEqual("http", result);

            
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration = CreateStaticConfiguration() with { LoadBalancerServicesHttps = true };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                staticConfiguration,
                emptyInstanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            result = testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceScheme();

            Assert.AreEqual("https", result);
        }

        [Test]
        public void CreateShardGroupAsync_ExceptionCreatingPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            DataElement dataElement = DataElement.Group;
            Int32 hashRangeStart = -10_000;
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockPersistentStorageManager.When((storageManager) => storageManager.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n10000")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_group_n10000");
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating persistent storage instance for data element type 'Group' and hash range start value -10000."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateShardGroupAsync_ExceptionCreatingEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid testBeginId3 = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            DataElement dataElement = DataElement.GroupToGroupMapping;
            Int32 hashRangeStart = 20_000;
            String persistentStorageInstanceName = "applicationaccesstest_grouptogroupmapping_20000";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>{ new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-20000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 }  } }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_20000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating event cache node for data element type 'GroupToGroupMapping' and hash range start value 20000 in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateShardGroupAsync_ExceptionCreatingReaderNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid testBeginId3 = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            Guid testBeginId4 = Guid.Parse("738650cb-eec3-40b2-8ffb-476e85084be1");
            Guid testBeginId5 = Guid.Parse("4fb4f095-2a6f-4f02-8cc6-e42700f69736");
            DataElement dataElement = DataElement.GroupToGroupMapping;
            Int32 hashRangeStart = 20_000;
            String persistentStorageInstanceName = "applicationaccesstest_grouptogroupmapping_20000";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-20000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-20000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-writer-20000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId4);
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId5);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-eventcache-20000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-reader-20000")), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-writer-20000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_20000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-eventcache-20000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating reader node for data element type 'GroupToGroupMapping' and hash range start value 20000 in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateShardGroupAsync_ExceptionCreatingWriterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid testBeginId3 = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            Guid testBeginId4 = Guid.Parse("738650cb-eec3-40b2-8ffb-476e85084be1");
            Guid testBeginId5 = Guid.Parse("4fb4f095-2a6f-4f02-8cc6-e42700f69736");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -400_000;
            String persistentStorageInstanceName = "applicationaccesstest_user_n400000";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId4);
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId5);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-reader-n400000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-writer-n400000")), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating writer node for data element type 'User' and hash range start value -400000 in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateShardGroupAsync()
        {
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid testBeginId3 = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            Guid testBeginId4 = Guid.Parse("738650cb-eec3-40b2-8ffb-476e85084be1");
            Guid testBeginId5 = Guid.Parse("4fb4f095-2a6f-4f02-8cc6-e42700f69736");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -400_000;
            String persistentStorageInstanceName = "applicationaccesstest_user_n400000";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId4);
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId5);
            mockPersistentStorageManager.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            TestPersistentStorageLoginCredentials result = await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);

            Assert.AreEqual(storageCredentials, result);
            mockPersistentStorageManager.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating shard group for data element 'User' and hash range start value -400000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'User' and hash range start value -400000...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating shard group.");
        }

        [Test]
        public async Task CreateShardGroupAsync_PersistentStorageCredentialsParameterNotNull()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("fae322df-92d1-4306-a8ef-c39ce1a2f084");
            Guid testBeginId3 = Guid.Parse("40f87f47-c586-42ea-905e-54de0e559944");
            Guid testBeginId4 = Guid.Parse("738650cb-eec3-40b2-8ffb-476e85084be1");
            Guid testBeginId5 = Guid.Parse("4fb4f095-2a6f-4f02-8cc6-e42700f69736");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -400_000;
            String persistentStorageInstanceName = "applicationaccesstest_user_n400000";
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceName}");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n400000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId4);
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId5);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            TestPersistentStorageLoginCredentials result = await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart, storageCredentials);

            Assert.AreEqual(storageCredentials, result);
            mockPersistentStorageManager.DidNotReceive().CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.DidNotReceive().Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.DidNotReceive().End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating shard group for data element 'User' and hash range start value -400000 in namespace 'default'...");
            mockApplicationLogger.DidNotReceive().Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'User' and hash range start value -400000...");
            mockApplicationLogger.DidNotReceive().Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating shard group.");
        }

        [Test]
        public void RestartShardGroupAsync_ExceptionScalingDown()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.GroupToGroupMapping;
            Int32 hashRangeStart = 1_073_741_823;
            mockMetricLogger.Begin(Arg.Any<ShardGroupRestartTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-eventcache-1073741823", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-reader-1073741823", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "grouptogroupmapping-writer-1073741823", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.RestartShardGroupAsync(dataElement, hashRangeStart);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupRestartTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupRestartTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling down shard group for data element 'GroupToGroupMapping' and hash range start value 1073741823 in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task RestartShardGroupAsync_ExceptionScalingUp()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.Group;
            Int32 hashRangeStart = 0;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupRestartTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-eventcache-0", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()), Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-reader-0", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()), Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-writer-0", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()), Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1PodList>(returnPods),
                Task.FromResult<V1PodList>(returnPods),
                Task.FromResult<V1PodList>(returnPods),
                Task.FromException<V1PodList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.RestartShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received().PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-eventcache-0", testNameSpace);
            await mockKubernetesClientShim.Received().PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-reader-0", testNameSpace);
            await mockKubernetesClientShim.Received().PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "group-writer-0", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedPodAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupRestartTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupRestartTime>());
            Assert.That(e.Message, Does.StartWith($"Error scaling up shard group for data element 'Group' and hash range start value 0 in namespace 'default'."));
        }

        [Test]
        public async Task RestartShardGroupAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -1;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n1" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n1" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n1" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupRestartTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n1", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n1", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n1", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.RestartShardGroupAsync(dataElement, hashRangeStart);

            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n1", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n1", testNameSpace);
            await mockKubernetesClientShim.Received(2).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n1", testNameSpace);
            await mockKubernetesClientShim.Received(3).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(3).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupRestartTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ShardGroupRestartTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupRestarted>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Restarting shard group for data element 'User' and hash range start value -1 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed restarting shard group.");
        }

        [Test]
        public async Task ScaleDownShardGroupAsync_ExceptionScalingReaderNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32"); 
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n3000000' to 0 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDownShardGroupAsync_ExceptionScalingWriterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-writer-n3000000' to 0 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDownShardGroupAsync_ExceptionWaitingForReaderOrWriterNode()
        {
            // TODO: Want to be able to create test ScaleDownShardGroupAsync_ExceptionWaitingForReaderNode(), but problem is that in the call to WaitForDeploymentScaleDownAsync(), 
            //   we can't tell whether the call is for the reader or writer... hence exception could end up being thrown for either, and we can't reliably assert the resulting
            //   exception message. 

            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedPodAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDownShardGroupAsync_ExceptionScalingEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedPodAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-eventcache-n3000000' to 0 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDownShardGroupAsync_ExceptionWaitingForEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1PodList>(returnPods),
                Task.FromResult<V1PodList>(returnPods),
                Task.FromException<V1PodList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(3).ListNamespacedPodAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-eventcache-n3000000' to scale down."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDownShardGroupAsync()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1PodList returnPods = new
            (
                new List<V1Pod>()
                {
                    new V1Pod()
                    {
                        Metadata = new V1ObjectMeta() { Name = "otherpod" }
                    }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleDownTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownShardGroupAsync(dataElement, hashRangeStart);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(3).ListNamespacedPodAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ShardGroupScaleDownTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupScaledDown>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Scaling down shard group for data element 'User' and hash range start value -3000000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed scaling down shard group.");
        }

        [Test]
        public async Task ScaleUpShardGroupAsync_ExceptionScalingEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-eventcache-n3000000' to 1 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleUpShardGroupAsync_ExceptionWaitingForEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-eventcache-n3000000' to become available."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleUpShardGroupAsync_ExceptionScalingReaderNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }, 
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n3000000' to 1 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleUpShardGroupAsync_ExceptionScalingWriterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromException<V1Scale>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-writer-n3000000' to 1 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleUpShardGroupAsync_ExceptionWaitingForReaderOrWriterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1DeploymentList>(returnDeployments),
                Task.FromResult<V1DeploymentList>(returnDeployments),
                Task.FromException<V1DeploymentList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received().ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleUpShardGroupAsync()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            DataElement dataElement = DataElement.User;
            Int32 hashRangeStart = -3_000_000;
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } },
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n3000000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupScaleUpTime>()).Returns(testBeginId);
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace).Returns(Task.FromResult<V1Scale>(new V1Scale()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleUpShardGroupAsync(dataElement, hashRangeStart);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-eventcache-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-reader-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), "user-writer-n3000000", testNameSpace);
            await mockKubernetesClientShim.Received(3).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ShardGroupScaleUpTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupScaledUp>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Scaling up shard group for data element 'User' and hash range start value -3000000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed scaling up shard group.");
        }

        [Test]
        public void DeleteShardGroupAsync_ExceptionDeletingReaderNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<ShardGroupDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, "user-reader-n100", "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteShardGroupAsync(DataElement.User, -100, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupDeleteTime>());
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteShardGroupAsync_ExceptionDeletingWriterNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<ShardGroupDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, "user-writer-n100", "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteShardGroupAsync(DataElement.User, -100, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupDeleteTime>());
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteShardGroupAsync_ExceptionDeletingEventCacheNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<ShardGroupDeleteTime>()).Returns(testBeginId);
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, "group-eventcache-n100", "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteShardGroupAsync(DataElement.Group, -100, false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupDeleteTime>());
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteShardGroupAsync_ExceptionDeletingPersistentStorageInstance()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_user_n100";
            mockMetricLogger.Begin(Arg.Any<ShardGroupDeleteTime>()).Returns(testBeginId);
            mockPersistentStorageManager.When((storageManager) => storageManager.DeletePersistentStorage(persistentStorageInstanceName)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteShardGroupAsync(DataElement.User, -100, true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ShardGroupDeleteTime>());
            Assert.That(e.Message, Does.StartWith($"Error deleting persistent storage instance '{persistentStorageInstanceName}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task DeleteShardGroupAsync()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            String persistentStorageInstanceName = "applicationaccesstest_group_120";
            mockMetricLogger.Begin(Arg.Any<ShardGroupDeleteTime>()).Returns(testBeginId);

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteShardGroupAsync(DataElement.Group, 120, true);

            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-reader-120", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-writer-120", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, "group-eventcache-120", "default");
            mockPersistentStorageManager.Received(1).DeletePersistentStorage(persistentStorageInstanceName);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ShardGroupDeleteTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupDeleted>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Deleting shard group for data element 'Group' and hash range start value 120...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, "Completed deleting shard group.");
        }

        [Test]
        public async Task CreateReaderNodeAsync_ExceptionCreatingNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeAsync(DataElement.User, -2_147_483_648, storageCredentials, eventCacheServiceUrl);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating reader node for data element type 'User' and hash range start value -2147483648 in namespace 'default'."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error creating reader deployment 'user-reader-n2147483648' in namespace 'default'."));
            Assert.That(e.InnerException.InnerException.Message, Does.StartWith($"Failed to create reader node Kubernetes deployment 'user-reader-n2147483648'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateReaderNodeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=grouptogroupmapping_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-reader-n2147483648" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<ReaderNodeCreateTime>()).Returns(testBeginId);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeAsync(DataElement.GroupToGroupMapping, -2_147_483_648, storageCredentials, eventCacheServiceUrl);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderNodeCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ReaderNodeCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ReaderNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating reader node for data element 'GroupToGroupMapping' and hash range start value -2147483648 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating reader node.");
        }

        [Test]
        public async Task CreateEventCacheNodeAsync_ExceptionCreatingNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-eventcache-100" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeAsync(DataElement.Group, 100);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventCacheNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<EventCacheNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating event cache node for data element type 'Group' and hash range start value 100 in namespace 'default'."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error creating event cache deployment 'group-eventcache-100' in namespace 'default'."));
            Assert.That(e.InnerException.InnerException.Message, Does.StartWith($"Failed to create event cache node Kubernetes deployment 'group-eventcache-100'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateEventCacheNodeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-eventcache-n100" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeAsync(DataElement.User, -100);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<EventCacheNodeCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventCacheNodeCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EventCacheNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating event cache node for data element 'User' and hash range start value -100 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating event cache node.");
        }

        [Test]
        public async Task CreateWriterNodeAsync_ExceptionCreatingNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n1");
            Uri eventCacheServiceUrl = new("http://user-writer-n1-service:5000");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "user-writer-n1" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeAsync(DataElement.User, -1, storageCredentials, eventCacheServiceUrl);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<WriterNodeCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<WriterNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating writer node for data element type 'User' and hash range start value -1 in namespace 'default'."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Error creating writer deployment 'user-writer-n1' in namespace 'default'."));
            Assert.That(e.InnerException.InnerException.Message, Does.StartWith($"Failed to create writer node Kubernetes deployment 'user-writer-n1'."));
            Assert.AreSame(mockException, e.InnerException.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterNodeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=group_0");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "group-writer-0" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockMetricLogger.Begin(Arg.Any<WriterNodeCreateTime>()).Returns(testBeginId);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeAsync(DataElement.Group, 0, storageCredentials, eventCacheServiceUrl);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<WriterNodeCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<WriterNodeCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<WriterNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating writer node for data element 'Group' and hash range start value 0 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating writer node.");
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccess_shard_configuration");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-coordinator" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<DistributedOperationCoordinatorNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeAsync(storageCredentials);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedOperationCoordinatorNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation coordinator deployment in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeAsync_ExceptionWaitingForDeployment()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccess_shard_configuration");
            mockMetricLogger.Begin(Arg.Any<DistributedOperationCoordinatorNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeAsync(storageCredentials);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedOperationCoordinatorNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error waiting for distributed operation coordinator deployment in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=applicationaccess_shard_configuration");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-coordinator" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<DistributedOperationCoordinatorNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeAsync(storageCredentials);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedOperationCoordinatorNodeCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedOperationCoordinatorNodeCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedOperationCoordinatorNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed operation coordinator node in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed operation coordinator node.");
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeAsync
                (
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648:5000/"),
                    new Uri("http://user-writer-n2147483648:5000/"),
                    Int32.MinValue,
                    Int32.MinValue,
                    new Uri("http://user-reader-0:5000/"),
                    new Uri("http://user-writer-0:5000/"),
                    0,
                    0,
                    false
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation router deployment."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeAsync
                (
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648:5000/"),
                    new Uri("http://user-writer-n2147483648:5000/"),
                    Int32.MinValue,
                    Int32.MinValue,
                    new Uri("http://user-reader-0:5000/"),
                    new Uri("http://user-writer-0:5000/"),
                    0,
                    0,
                    false
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating operation router service 'operation-router-service'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeAsync_ExceptionWaitingForDeployment()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeAsync
                (
                    DataElement.User,
                    new Uri("http://user-reader-n2147483648:5000/"),
                    new Uri("http://user-writer-n2147483648:5000/"),
                    Int32.MinValue,
                    Int32.MinValue,
                    new Uri("http://user-reader-0:5000/"),
                    new Uri("http://user-writer-0:5000/"),
                    0,
                    0,
                    false
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error waiting for distributed operation router deployment to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "operation-router" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            mockMetricLogger.Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterNodeAsync
            (
                DataElement.User,
                new Uri("http://user-reader-n2147483648:5000/"),
                new Uri("http://user-writer-n2147483648:5000/"),
                Int32.MinValue,
                Int32.MinValue,
                new Uri("http://user-reader-0:5000/"),
                new Uri("http://user-writer-0:5000/"),
                0,
                0,
                false
            );

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedOperationRouterNodeCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedOperationRouterNodeCreated>());
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Creating distributed operation router node...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager,  ApplicationLogging.LogLevel.Information, "Completed creating distributed operation router node.");
        }

        [Test]
        public void CreateApplicationAccessNodeAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String deploymentName = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = deploymentName }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            Func<Task> createDeploymentFunction = () => Task.FromException(mockException);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "reader", 10_000);
            });

            Assert.That(e.Message, Does.StartWith($"Error creating reader deployment 'user-reader-n2147483648' in namespace 'default'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void CreateApplicationAccessNodeAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            String deploymentName = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = deploymentName }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            Func<Task> createDeploymentFunction = () => Task.CompletedTask;
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "reader", 10_000);
            });

            Assert.That(e.Message, Does.StartWith($"Error creating reader service 'user-reader-n2147483648-service' in namespace 'default'."));
        }

        [Test]
        public void CreateApplicationAccessNodeAsync_ExceptionWaitingForDeployment()
        {
            var mockException = new Exception("Mock exception");
            String deploymentName = "user-writer-2147483647";
            Func<Task> createDeploymentFunction = () => Task.CompletedTask;
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "writer", 10_000);
            });

            Assert.That(e.Message, Does.StartWith($"Error waiting for writer deployment 'user-writer-2147483647' in namespace 'default' to become available."));
        }

        [Test]
        public async Task CreateApplicationAccessNodeAsync()
        {
            String deploymentName = "group-eventcache-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = deploymentName }, Status = new V1DeploymentStatus { AvailableReplicas = 1 } }
                }
            );
            Func<Task> createDeploymentFunction = () => Task.CompletedTask;
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, "event cache", 10_000);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
        }

        [Test]
        public void DeleteApplicationAccessNodeAsync_ExceptionDeletingService()
        {
            var mockException = new Exception("Mock exception");
            String deploymentName = "user-writer-2147483647";
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, $"{deploymentName}-service", "default").Returns(Task.FromException<V1Service>(mockException));
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, deploymentName, "default").Returns(Task.FromResult(new V1Status()));
            
            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteApplicationAccessNodeAsync(deploymentName, "writer");
            });

            Assert.That(e.Message, Does.StartWith($"Error deleting writer service 'user-writer-2147483647-service'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public void DeleteApplicationAccessNodeAsync_ExceptionDeletingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String deploymentName = "user-writer-2147483647";
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, $"{deploymentName}-service", "default").Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, deploymentName, "default").Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteApplicationAccessNodeAsync(deploymentName, "writer");
            });

            Assert.That(e.Message, Does.StartWith($"Error deleting writer deployment 'user-writer-2147483647'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task DeleteApplicationAccessNodeAsync()
        {
            String deploymentName = "user-writer-2147483647";
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, $"{deploymentName}-service", "default").Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, deploymentName, "default").Returns(Task.FromResult(new V1Status()));

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteApplicationAccessNodeAsync(deploymentName, "writer");

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, $"{deploymentName}-service", "default");
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, deploymentName, "default");
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerServiceAsync_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerServiceAsync_ExceptionRetrievingServiceIpAddress()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-coordinator-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1ServiceList>(returnServices),
                Task.FromException<V1ServiceList>(mockException)
            );

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerServiceAsync()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = "operation-coordinator-externalservice" },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>()); 
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Creating load balancer service for distributed operation coordinator on port 7001 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(testKubernetesDistributedAccessManagerInstanceManager, ApplicationLogging.LogLevel.Information, $"Completed creating load balancer service.");
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public async Task CreateClusterIpServiceAsync_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            String appLabelValue = "user-eventcache-n2147483648";
            String serviceNamePostfix = "-service";
            UInt16 port = 5000;
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateClusterIpServiceAsync(appLabelValue, serviceNamePostfix, port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create Kubernetes 'ClusterIP' service '{appLabelValue}{serviceNamePostfix}' for pod '{appLabelValue}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateClusterIpServiceAsync()
        {
            String appLabelValue = "user-eventcache-n2147483648";
            String serviceNamePostfix = "-service";
            UInt16 port = 5000;
            V1Service capturedServiceDefinition = null;
            await mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Do<V1Service>(argumentValue => capturedServiceDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateClusterIpServiceAsync(appLabelValue, serviceNamePostfix, port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            Assert.AreEqual($"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}", capturedServiceDefinition.ApiVersion);
            Assert.AreEqual(V1Service.KubeKind, capturedServiceDefinition.Kind);
            Assert.AreEqual($"{appLabelValue}{serviceNamePostfix}", capturedServiceDefinition.Metadata.Name);
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
            String serviceNamePostfix = "-externalservice";
            UInt16 port = 7000;
            UInt16 targetPort = 5000;
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateLoadBalancerServiceAsync(appLabelValue, serviceNamePostfix, port, targetPort);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create Kubernetes 'LoadBalancer' service '{appLabelValue}{serviceNamePostfix}' for pod '{appLabelValue}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateLoadBalancerServiceAsync()
        {
            String appLabelValue = "operation-coordinator";
            String serviceNamePostfix = "-externalservice";
            UInt16 port = 7000;
            UInt16 targetPort = 5000;
            V1Service capturedServiceDefinition = null;
            await mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Do<V1Service>(argumentValue => capturedServiceDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateLoadBalancerServiceAsync(appLabelValue, serviceNamePostfix, port, targetPort);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            Assert.AreEqual($"{V1Service.KubeGroup}/{V1Service.KubeApiVersion}", capturedServiceDefinition.ApiVersion);
            Assert.AreEqual(V1Service.KubeKind, capturedServiceDefinition.Kind);
            Assert.AreEqual($"{appLabelValue}{serviceNamePostfix}", capturedServiceDefinition.Metadata.Name);
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
        public async Task UpdateServiceAsync_ExceptionUpdatingService()
        {
            var mockException = new Exception("Mock exception");
            String existingAppLabelValue = "writer";
            String serviceNamePostfix = "-externalservice";
            String newAppLabelValue = "user-writer-n2147483648-service";
            mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), $"{existingAppLabelValue}{serviceNamePostfix}", testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.UpdateServiceAsync($"{existingAppLabelValue}{serviceNamePostfix}", newAppLabelValue);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), $"{existingAppLabelValue}{serviceNamePostfix}", testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to update Kubernetes service '{existingAppLabelValue}{serviceNamePostfix}' to target pod '{newAppLabelValue}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task UpdateServiceAsync()
        {
            String existingAppLabelValue = "writer";
            String serviceNamePostfix = "-externalservice";
            String newAppLabelValue = "user-writer-n2147483648-service";
            String expectedPatchContentString = $"{{\"spec\":{{\"selector\":{{\"app\":\"user-writer-n2147483648-service\"}}}}}}";
            V1Patch capturedPatchDefinition = null;
            await mockKubernetesClientShim.PatchNamespacedServiceAsync(null, Arg.Do<V1Patch>(argumentValue => capturedPatchDefinition = argumentValue), $"{existingAppLabelValue}{serviceNamePostfix}", testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.UpdateServiceAsync($"{existingAppLabelValue}{serviceNamePostfix}", newAppLabelValue);

            await mockKubernetesClientShim.Received(1).PatchNamespacedServiceAsync(null, Arg.Any<V1Patch>(), $"{existingAppLabelValue}{serviceNamePostfix}", testNameSpace);
            Assert.IsAssignableFrom<V1Service>(capturedPatchDefinition.Content);
            var serviceContent = (V1Service)capturedPatchDefinition.Content;
            Assert.IsTrue(serviceContent.Spec.Selector.ContainsKey("app"));
            Assert.AreEqual(newAppLabelValue, serviceContent.Spec.Selector["app"]);
        }

        [Test]
        public async Task DeleteServiceAsync_ExceptionDeletingService()
        {
            var mockException = new Exception("Mock exception");
            String name = "operation-router-service";
            mockKubernetesClientShim.DeleteNamespacedServiceAsync(null, name, testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteServiceAsync(name);
            });

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, name, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to delete Kubernetes service 'operation-router-service'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task DeleteServiceAsync()
        {
            String name = "operation-router-service";

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteServiceAsync(name);

            await mockKubernetesClientShim.Received(1).DeleteNamespacedServiceAsync(null, name, testNameSpace);
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync_ExceptionRetrievingServices()
        {
            var mockException = new Exception("Mock exception");
            String serviceName = "operation-router-externalservice";
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve load balancer service '{serviceName}' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync_LoadBalancerServiceNotFound()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Could not find load balancer service '{serviceName}' in namespace '{testNameSpace}'."));
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync_IngressPointNull()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service() 
                    { 
                        Metadata = new V1ObjectMeta() { Name = serviceName }, 
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Load balancer service '{serviceName}' in namespace '{testNameSpace}' did not contain an ingress point."));
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync_IngressPointListEmpty()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Load balancer service '{serviceName}' in namespace '{testNameSpace}' did not contain an ingress point."));
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync_FailureToConvertIngressIpToIPAddress()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "notvalid" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to convert ingress 'Ip' property 'notvalid' to an IP address, for load balancer service '{serviceName}' in namespace '{testNameSpace}'."));
        }

        [Test]
        public async Task GetLoadBalancerServiceIpAddressAsync()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.GetLoadBalancerServiceIpAddressAsync(serviceName);

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
        }

        [Test]
        public void CreateReaderNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for reader nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for reader nodes."));
        }

        [Test]
        public async Task CreateReaderNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create reader node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateReaderNodeDeploymentAsync()
        {
            String name = "user-reader-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateReaderNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
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
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.EventCacheNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for event cache nodes."));
        }

        [Test]
        public async Task CreateEventCacheNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-eventcache-n2147483648";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create event cache node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateEventCacheNodeDeploymentAsync()
        {
            String name = "user-eventcache-n2147483648";
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateEventCacheNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateEventCacheNodeDeploymentAsync(name);

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
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
            Assert.AreEqual(6, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.FailureThreshold);
            Assert.AreEqual(4, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.PeriodSeconds);
        }

        [Test]
        public void CreateWriterNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventPersistence");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventPersistence' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for writer nodes."));
        }

        [Test]
        public async Task CreateWriterNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create writer node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateWriterNodeDeploymentAsync()
        {
            String name = "user-writer-n2147483648";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateWriterNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventPersistence"]["EventPersisterBackupFilePath"] = "/eventbackup/user-writer-n2147483648-eventbackup.json";
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
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
            Assert.AreEqual(7, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.FailureThreshold);
            Assert.AreEqual(5, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.PeriodSeconds);
        }

        [Test]
        public void CreateDistributedOperationCoordinatorNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=AccessManagerConfiguration");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for distributed operation coordinator nodes."));
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=AccessManagerConfiguration");
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed operation coordinator node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync()
        {
            String name = "operation-coordinator";
            TestPersistentStorageLoginCredentials storageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=AccessManagerConfiguration");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["DatabaseConnection"]["SqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
            ));
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, storageCredentials);

            mockAppSettingsConfigurer.Received(1).ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Any<JObject>());
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
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
            Assert.AreEqual(8, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.FailureThreshold);
            Assert.AreEqual(6, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.PeriodSeconds);
        }

        [Test]
        public void CreateDistributedOperationRouterNodeDeploymentAsync_AppSettingsConfigurationTemplateMissingProperties()
        {
            String name = "operation-router";
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("ShardRouting");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'ShardRouting' was not found in JSON document containing appsettings configuration for distributed operation router nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.DistributedOperationRouterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageManager, mockPersistentStorageInstanceRandomNameGenerator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'MetricLogging' was not found in JSON document containing appsettings configuration for distributed operation router nodes."));
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeDeploymentAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "operation-router";
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));

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
                    true
                );
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to create distributed operation router node Kubernetes deployment '{name}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterNodeDeploymentAsync()
        {
            String name = "operation-router";
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
            await mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Do<V1Deployment>(argumentValue => capturedDeploymentDefinition = argumentValue), testNameSpace);

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
                true
            );

            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace);
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
            Assert.AreEqual(9, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.FailureThreshold);
            Assert.AreEqual(7, capturedDeploymentDefinition.Spec.Template.Spec.Containers[0].StartupProbe.PeriodSeconds);
        }

        [Test]
        public void ScaleDeploymentAsync_ReplicaCountParameter0()
        {
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, -1);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'replicaCount' with value -1 must be greater than or equal to 0."));
            Assert.AreEqual("replicaCount", e.ParamName);
        }

        [Test]
        public async Task ScaleDeploymentAsync_ExceptionPatchingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, testNameSpace).Returns(Task.FromException<V1Scale>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, 0);
            });

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n2147483648' to 0 replica(s)."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDeploymentAsync()
        {
            String name = "user-reader-n2147483648";
            V1Patch capturedPatchDefinition = null;
            await mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Do<V1Patch>(argumentValue => capturedPatchDefinition = argumentValue), name, testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, 3);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, testNameSpace);
            Assert.IsAssignableFrom<V1Deployment>(capturedPatchDefinition.Content);
            var deploymentContent = (V1Deployment)capturedPatchDefinition.Content;
            Assert.AreEqual(3, deploymentContent.Spec.Replicas);
        }

        [Test]
        public void WaitForLoadBalancerServiceAsync_CheckIntervalParameter0()
        {
            String serviceName = "operation-router-externalservice";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForLoadBalancerServiceAsync(serviceName, 0, 2000);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'checkInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("checkInterval", e.ParamName);
        }

        [Test]
        public void WaitForLoadBalancerServiceAsync_AbortTimeoutParameter0()
        {
            String serviceName = "operation-router-externalservice";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForLoadBalancerServiceAsync(serviceName, 100, 0);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'abortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("abortTimeout", e.ParamName);
        }

        [Test]
        public void WaitForLoadBalancerServiceAsync_AbortTimeoutExpires()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList returnServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromResult<V1ServiceList>(returnServices));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForLoadBalancerServiceAsync(serviceName, 100, 500);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for load balancer service '{serviceName}' in namespace '{testNameSpace}' to become available."));
        }

        [Test]
        public async Task WaitForLoadBalancerServiceAsync_ExceptionGettingDeployments()
        {
            var mockException = new Exception("Mock exception");
            String serviceName = "operation-router-externalservice";
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForLoadBalancerServiceAsync(serviceName, 100, 500);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to wait for load balancer service '{serviceName}' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task WaitForLoadBalancerServiceAsync()
        {
            String serviceName = "operation-router-externalservice";
            V1ServiceList unavailableServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                            }
                        }
                    }
                }
            );
            V1ServiceList availableServices = new
            (
                new List<V1Service>
                {
                    new V1Service() { Metadata = new V1ObjectMeta() { Name = "OtherService" } },
                    new V1Service()
                    {
                        Metadata = new V1ObjectMeta() { Name = serviceName },
                        Status = new V1ServiceStatus()
                        {
                            LoadBalancer = new V1LoadBalancerStatus()
                            {
                                Ingress = new List<V1LoadBalancerIngress>()
                                {
                                    new V1LoadBalancerIngress() { Ip = "10.104.198.18" }
                                }
                            }
                        }
                    }
                }
            );
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1ServiceList>(unavailableServices),
                Task.FromResult<V1ServiceList>(unavailableServices),
                Task.FromResult<V1ServiceList>(unavailableServices),
                Task.FromResult<V1ServiceList>(availableServices)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForLoadBalancerServiceAsync(serviceName, 100, 500);

            await mockKubernetesClientShim.Received(4).ListNamespacedServiceAsync(null, testNameSpace);
        }

        [Test]
        public void WaitForDeploymentAvailabilityAsync_AbortTimeoutExpires()
        {
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
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, 100, 500);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for Kubernetes deployment 'user-reader-n2147483648' to become available."));
            Assert.That(e.InnerException.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for deployment predicate to return true."));
        }

        [Test]
        public async Task WaitForDeploymentAvailabilityAsync_ExceptionGettingDeployments()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromException<V1DeploymentList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, 100, 500);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-reader-n2147483648' to become available."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task WaitForDeploymentAvailabilityAsync()
        {
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
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(unavailableDeployments),
                Task.FromResult<V1DeploymentList>(availableDeployments)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentAvailabilityAsync(name, 100, 1000);

            await mockKubernetesClientShim.Received(4).ListNamespacedDeploymentAsync(null, testNameSpace);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_CheckIntervalParameter0()
        {
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, 0, 2000);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'checkInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("checkInterval", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_AbortTimeoutParameter0()
        {
            String name = "user-reader-n2147483648";

            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, 100, 0);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'abortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("abortTimeout", e.ParamName);
        }

        [Test]
        public async Task WaitForDeploymentScaleDownAsync_ExceptionGettingPods()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n2147483648";
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromException<V1PodList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, 100, 2000);
            });

            await mockKubernetesClientShim.Received(1).ListNamespacedPodAsync(null, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to wait for Kubernetes deployment 'user-reader-n2147483648' to scale down."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void WaitForDeploymentScaleDownAsync_AbortTimeoutExpires()
        {
            String name = "user-reader-n2147483648";
            V1PodList returnPods = new
            (
                new List<V1Pod>
                {
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = "OtherPod" } },
                    new V1Pod() { Metadata = new V1ObjectMeta() { Name = name } }
                }
            );
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns(Task.FromResult<V1PodList>(returnPods));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, 100, 500);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 500 milliseconds expired while waiting for Kubernetes deployment 'user-reader-n2147483648' to scale down."));
        }

        [Test]
        public async Task WaitForDeploymentScaleDownAsync()
        {
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
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsAfterScaleDown)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentScaleDownAsync(name, 100, 1000);

            await mockKubernetesClientShim.Received(4).ListNamespacedPodAsync(null, testNameSpace);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_CheckIntervalParameter0()
        {
            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync((deployment) => { return true; }, 0, 2000);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'checkInterval' with value 0 must be greater than 0."));
            Assert.AreEqual("checkInterval", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_AbortTimeoutParameter0()
        {
            var e = Assert.ThrowsAsync<ArgumentOutOfRangeException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync((deployment) => { return true; }, 100, 0);
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'abortTimeout' with value 0 must be greater than 0."));
            Assert.AreEqual("abortTimeout", e.ParamName);
        }

        [Test]
        public void WaitForDeploymentPredicateAsync_AbortTimeoutExpires()
        {
            String name = "user-reader-n2147483648";
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>()
                {
                    new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "OtherDeployment" } }
                }
            );
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<DeploymentPredicateWaitTimeoutExpiredException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync((deployment) => { return false; }, 100, 1000);
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
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            e = Assert.ThrowsAsync<DeploymentPredicateWaitTimeoutExpiredException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync((deployment) => { return false; }, 100, 1000);
            });

            Assert.That(e.Message, Does.StartWith($"Timeout value of 1000 milliseconds expired while waiting for deployment predicate to return true."));
            Assert.AreEqual(1000, e.Timeout);
        }

        [Test]
        public async Task WaitForDeploymentPredicateAsync()
        {
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
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            await testKubernetesDistributedAccessManagerInstanceManager.WaitForDeploymentPredicateAsync(predicate, 100, 1000);

            await mockKubernetesClientShim.Received(5).ListNamespacedDeploymentAsync(null, testNameSpace);
        }

        [Test]
        public async Task DeleteDeploymentAsync_ExceptionDeletingDeployment()
        {
            var mockException = new Exception("Mock exception");
            String name = "user-reader-n3000000";
            mockKubernetesClientShim.DeleteNamespacedDeploymentAsync(null, name, testNameSpace).Returns(Task.FromException<V1Status>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.DeleteDeploymentAsync(name);
            });

            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, name, testNameSpace);
            Assert.That(e.Message, Does.StartWith($"Failed to delete Kubernetes deployment 'user-reader-n3000000'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task DeleteDeploymentAsync()
        {
            String name = "user-reader-n3000000";

            await testKubernetesDistributedAccessManagerInstanceManager.DeleteDeploymentAsync(name);

            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, name, testNameSpace);
        }

        [Test]
        public async Task ScaleDownAndDeleteDeploymentAsync()
        {
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
            mockKubernetesClientShim.ListNamespacedPodAsync(null, testNameSpace).Returns
            (
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsBeforeScaleDown),
                Task.FromResult<V1PodList>(podsAfterScaleDown)
            );

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleDownAndDeleteDeploymentAsync(name, 100, 1000);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, testNameSpace);
            await mockKubernetesClientShim.Received(4).ListNamespacedPodAsync(null, testNameSpace);
            await mockKubernetesClientShim.Received(1).DeleteNamespacedDeploymentAsync(null, name, testNameSpace);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a <see cref="KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers"/> preconfigured with load balancer services.
        /// </summary>
        protected KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers CreateInstanceManagerWithLoadBalancerServices()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
            };
            return new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageManager,
                mockPersistentStorageInstanceRandomNameGenerator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction, 
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
        }

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
        /// Creates a test <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/> instance.
        /// </summary>
        /// <returns>The test <see cref="KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration"/> instance.</returns>
        protected KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration CreateStaticConfiguration()
        {
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration configuration = new()
            {
                PodPort = 5000,
                ExternalPort = 7000,
                NameSpace = testNameSpace, 
                PersistentStorageInstanceNamePrefix = "applicationaccesstest",
                LoadBalancerServicesHttps = false,
                DeploymentWaitPollingInterval = 100,
                ServiceAvailabilityWaitAbortTimeout = 5000,
                DistributedOperationCoordinatorRefreshIntervalWaitBuffer = 1000,
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
                    AppSettingsConfigurationTemplate = CreateWriterNodeAppSettingsConfigurationTemplate(),
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
                    AppSettingsConfigurationTemplate = CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate(),
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
                    AppSettingsConfigurationTemplate = CreateDistributedOperationRouterNodeAppSettingsConfigurationTemplate(),
                    CpuResourceRequest = "400m",
                    MemoryResourceRequest = "450Mi",
                    StartupProbeFailureThreshold = 9,
                    StartupProbePeriod = 7
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
                ""DatabaseConnection"":{
                    ""SqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""RetryCount"": 10,
                            ""RetryInterval"": 20,
                            ""OperationTimeout"": 0
                        }
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
                    ""Enabled"": true,
                    ""BufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 500,
                        ""DequeueOperationLoopInterval"": 30000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""SqlDatabaseConnection"": {
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
                    ""Enabled"": false,
                    ""BufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedBufferProcessor"",
                        ""BufferSizeLimit"": 501,
                        ""DequeueOperationLoopInterval"": 30001,
                        ""BufferProcessingFailureAction"": ""DisableMetricLogging""
                    },
                    ""SqlDatabaseConnection"": {
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
                ""DatabaseConnection"":{
                    ""SqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""RetryCount"": 4,
                            ""RetryInterval"": 5,
                            ""OperationTimeout"": 120000
                        }
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
                    ""Enabled"": true,
                    ""BufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 1000,
                        ""DequeueOperationLoopInterval"": 45000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""SqlDatabaseConnection"": {
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
                ""DatabaseConnection"":{
                    ""SqlDatabaseConnection"": {
                        ""DatabaseType"": ""SqlServer"",
                        ""ConnectionParameters"": {
                            ""RetryCount"": 3,
                            ""RetryInterval"": 9,
                            ""OperationTimeout"": 120000
                        }
                    }
                },
                ""ShardConfigurationRefresh"": {
                    ""RefreshInterval"": ""11000""
                },
                ""ShardConnection"": {
                    ""RetryCount"": ""3"",
                    ""RetryInterval"": ""11"",
                    ""ConnectionTimeout"": 300000
                },
                ""MetricLogging"": {
                    ""Enabled"": true,
                    ""BufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 2000,
                        ""DequeueOperationLoopInterval"": 46000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""SqlDatabaseConnection"": {
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
                    ""ConnectionTimeout"": 300000
                },
                ""MetricLogging"": {
                    ""Enabled"": true,
                    ""BufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 250,
                        ""DequeueOperationLoopInterval"": 2000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""SqlDatabaseConnection"": {
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

        /// <summary>
        /// Creates a test <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.
        /// </summary>
        /// <param name="persistentStorageInstanceNamePrefix">Prefix in names for persistent instances.</param>
        /// <returns>The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.</returns>
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> CreateInstanceConfiguration(String persistentStorageInstanceNamePrefix)
        {
            if (persistentStorageInstanceNamePrefix != "")
            {
                persistentStorageInstanceNamePrefix = $"{persistentStorageInstanceNamePrefix}_";
            }

            return new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                Writer1Url = new Uri("http://10.104.198.19:7001/"),
                Writer2Url = new Uri("http://10.104.198.20:7001/"),
                ShardConfigurationPersistentStorageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog=Dummy"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                },
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}grouptogroup_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                    )
                },
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    )
                },
                DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/")
            };
        }

        /// <summary>
        /// Creates a test <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance with 2 user shard groups (and a persistent storage instance name prefix 'applicationaccesstest').
        /// </summary>
        /// <param name="persistentStorageInstanceNamePrefix">Prefix in names for persistent instances.</param>
        /// <returns>The <see cref="KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration{TPersistentStorageCredentials}"/> instance.</returns>
        /// <remarks>Used for testing method MergeShardGroupsAsync(), since multiple shard groups are required to merge.</remarks>
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> CreateTwoUserShardGroupInstanceConfiguration(String persistentStorageInstanceNamePrefix)
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> returnConfiguration = CreateInstanceConfiguration(persistentStorageInstanceNamePrefix);
            if (persistentStorageInstanceNamePrefix != "")
            {
                persistentStorageInstanceNamePrefix = $"{persistentStorageInstanceNamePrefix}_";
            }
            returnConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0-service:5000/"))
                )
            };
            returnConfiguration.GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}grouptogroup_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                )
            };
            returnConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials($"Server=127.0.0.1;User Id=sa;Password=password;Initial Catalog={persistentStorageInstanceNamePrefix}_group_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                )
            };

            return returnConfiguration;
        }

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a <see cref="V1Deployment"/> has a specified 'app' label name.
        /// </summary>
        /// <param name="expectedAppName">The expected 'app' label name.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the NSubstitute Arg.Any{T} argument matcher.</remarks>
        protected Expression<Predicate<V1Deployment>> DeploymentWithAppName(String expectedAppName)
        {
            return (V1Deployment actualDeployment) => actualDeployment.Spec.Selector.MatchLabels["app"] == expectedAppName;
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the KubernetesDistributedAccessManagerInstanceManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        protected class KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers : KubernetesDistributedAccessManagerInstanceManager<TestPersistentStorageLoginCredentials>
        {
            #pragma warning disable 1591

            public KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials> UserShardGroupConfigurationSet
            {
                get { return userShardGroupConfigurationSet; }
            }

            public KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials> GroupToGroupMappingShardGroupConfigurationSet
            {
                get { return groupToGroupMappingShardGroupConfigurationSet; }
            }

            public KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials> GroupShardGroupConfigurationSet
            {
                get { return groupShardGroupConfigurationSet; }
            }

            #pragma warning restore 1591

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
            /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="persistentStorageInstanceRandomNameGenerator">Random name generator for persistent storage instances.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                IDistributedAccessManagerPersistentStorageManager<TestPersistentStorageLoginCredentials> persistentStorageManager,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Configuration for the instance manager.</param>
            /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
            /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="persistentStorageInstanceRandomNameGenerator">Random name generator for persistent storage instances.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration,
                IDistributedAccessManagerPersistentStorageManager<TestPersistentStorageLoginCredentials> persistentStorageManager,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, instanceConfiguration, persistentStorageManager, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Configuration for the instance manager.</param>
            /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
            /// <param name="persistentStorageManager">Used to manage instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="persistentStorageInstanceRandomNameGenerator">Random name generator for persistent storage instances.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersisterCreationFunction">A function used to create the persister used to write shard configuration to persistent storage.  Accepts TPersistentStorageCredentials and returns an <see cref="IShardConfigurationSetPersister{TClientConfiguration, TJsonSerializer}"/> instance.</param>
            /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration,
                IDistributedAccessManagerPersistentStorageManager<TestPersistentStorageLoginCredentials> persistentStorageManager,
                IPersistentStorageInstanceRandomNameGenerator persistentStorageInstanceRandomNameGenerator,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IKubernetesClientShim kubernetesClientShim,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, instanceConfiguration, persistentStorageManager, persistentStorageInstanceRandomNameGenerator, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, kubernetesClientShim, logger, metricLogger)
            {
            }

            #pragma warning disable 1591

            public new Task<IPAddress> CreateWriterLoadBalancerServiceAsync(String appLabelValue, UInt16 port)
            {
                return base.CreateWriterLoadBalancerServiceAsync(appLabelValue, port);
            }

            public new async Task SplitShardGroupAsync
            (
                DataElement dataElement,
                Int32 hashRangeStart,
                Int32 splitHashRangeStart,
                Int32 splitHashRangeEnd,
                Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
                Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
                Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventDeleter> sourceShardGroupEventDeleterCreationFunction,
                Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
                Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
                Int32 eventBatchSize,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval,
                IDistributedAccessManagerShardGroupSplitter shardGroupSplitter
            )
            {
                await base.SplitShardGroupAsync
                (
                    dataElement, 
                    hashRangeStart, 
                    splitHashRangeStart, 
                    splitHashRangeEnd,
                    sourceShardGroupEventReaderCreationFunction,
                    targetShardGroupEventPersisterCreationFunction,
                    sourceShardGroupEventDeleterCreationFunction,
                    operationRouterCreationFunction,
                    sourceShardGroupWriterAdministratorCreationFunction,
                    eventBatchSize,
                    sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    sourceWriterNodeOperationsCompleteCheckRetryInterval,
                    shardGroupSplitter
                );
            }

            public new async Task MergeShardGroupsAsync
            (
                DataElement dataElement,
                Int32 sourceShardGroup1HashRangeStart,
                Int32 sourceShardGroup2HashRangeStart,
                Int32 sourceShardGroup2HashRangeEnd,
                Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBatchReader> sourceShardGroupEventReaderCreationFunction,
                Func<TestPersistentStorageLoginCredentials, IAccessManagerTemporalEventBulkPersister<String, String, String, String>> targetShardGroupEventPersisterCreationFunction,
                Func<Uri, IDistributedAccessManagerOperationRouter> operationRouterCreationFunction,
                Func<Uri, IDistributedAccessManagerWriterAdministrator> sourceShardGroupWriterAdministratorCreationFunction,
                Int32 eventBatchSize,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                Int32 sourceWriterNodeOperationsCompleteCheckRetryInterval,
                IDistributedAccessManagerShardGroupMerger shardGroupMerger
            )
            {
                await base.MergeShardGroupsAsync
                (
                    dataElement,
                    sourceShardGroup1HashRangeStart,
                    sourceShardGroup2HashRangeStart,
                    sourceShardGroup2HashRangeEnd,
                    sourceShardGroupEventReaderCreationFunction,
                    targetShardGroupEventPersisterCreationFunction,
                    operationRouterCreationFunction,
                    sourceShardGroupWriterAdministratorCreationFunction,
                    eventBatchSize,
                    sourceWriterNodeOperationsCompleteCheckRetryAttempts,
                    sourceWriterNodeOperationsCompleteCheckRetryInterval,
                    shardGroupMerger
                );
            }

            public new void UpdateAndPersistShardConfigurationSets
            (
                DataElement dataElement,
                IEnumerable<HashRangeStartAndClientConfigurations> configurationUpdates,
                IEnumerable<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> configurationAdditions,
                IEnumerable<Int32> configurationDeletes
            )
            {
                base.UpdateAndPersistShardConfigurationSets(dataElement, configurationUpdates, configurationAdditions, configurationDeletes);
            }

            public new Int32 GetDistributedOperationCoordinatorConfigurationRefreshInterval()
            {
                return base.GetDistributedOperationCoordinatorConfigurationRefreshInterval();
            }

            public new String GenerateNodeIdentifier(DataElement dataElement, NodeType nodeType, Int32 hashRangeStart)
            {
                return base.GenerateNodeIdentifier(dataElement, nodeType, hashRangeStart);
            }

            public new String GeneratePersistentStorageInstanceName(DataElement dataElement, Int32 hashRangeStart)
            {
                return base.GeneratePersistentStorageInstanceName(dataElement, hashRangeStart);
            }

            public new String GetLoadBalancerServiceScheme()
            {
                return base.GetLoadBalancerServiceScheme();
            }

            public new async Task<TestPersistentStorageLoginCredentials> CreateShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, TestPersistentStorageLoginCredentials persistentStorageCredentials = null)
            {
                return await base.CreateShardGroupAsync(dataElement, hashRangeStart, persistentStorageCredentials);
            }

            public new async Task RestartShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
            {
                await base.RestartShardGroupAsync(dataElement, hashRangeStart);
            }

            public new async Task ScaleDownShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
            {
                await base.ScaleDownShardGroupAsync(dataElement, hashRangeStart);
            }

            public new async Task ScaleUpShardGroupAsync(DataElement dataElement, Int32 hashRangeStart)
            {
                await base.ScaleUpShardGroupAsync(dataElement, hashRangeStart);
            }

            public new async Task DeleteShardGroupAsync(DataElement dataElement, Int32 hashRangeStart, Boolean deletePersistentStorageInstance)
            {
                await base.DeleteShardGroupAsync(dataElement, hashRangeStart, deletePersistentStorageInstance);
            }

            public new async Task CreateReaderNodeAsync(DataElement dataElement, Int32 hashRangeStart, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
            {
                await base.CreateReaderNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl);
            }

            public new async Task CreateEventCacheNodeAsync(DataElement dataElement, Int32 hashRangeStart)
            {
                await base.CreateEventCacheNodeAsync(dataElement, hashRangeStart);
            }

            public new async Task CreateWriterNodeAsync(DataElement dataElement, Int32 hashRangeStart, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
            {
                await base.CreateWriterNodeAsync(dataElement, hashRangeStart, persistentStorageCredentials, eventCacheServiceUrl);
            }

            public new async Task CreateDistributedOperationCoordinatorNodeAsync(TestPersistentStorageLoginCredentials shardConfigurationPersistentStorageCredentials)
            {
                await base.CreateDistributedOperationCoordinatorNodeAsync(shardConfigurationPersistentStorageCredentials);
            }

            public new async Task CreateDistributedOperationRouterNodeAsync
            (
                DataElement dataElement,
                Uri sourceReaderUrl,
                Uri sourceWriterUrl,
                Int32 sourceHashRangeStart,
                Int32 sourceHashRangeEnd,
                Uri targetReaderUrl,
                Uri targetWriterUrl,
                Int32 targetHashRangeStart,
                Int32 targetHashRangeEnd,
                Boolean routingInitiallyOn
            )
            {
                await base.CreateDistributedOperationRouterNodeAsync
                (
                    dataElement,
                    sourceReaderUrl,
                    sourceWriterUrl,
                    sourceHashRangeStart,
                    sourceHashRangeEnd,
                    targetReaderUrl,
                    targetWriterUrl,
                    targetHashRangeStart,
                    targetHashRangeEnd,
                    routingInitiallyOn
                );
            }

            public new async Task CreateApplicationAccessNodeAsync(String deploymentName, Func<Task> createDeploymentFunction, String nodeTypeName, Int32 abortTimeout)
            {
                await base.CreateApplicationAccessNodeAsync(deploymentName, createDeploymentFunction, nodeTypeName, abortTimeout);
            }

            public new async Task DeleteApplicationAccessNodeAsync(String deploymentName, String nodeTypeName)
            {
                await base.DeleteApplicationAccessNodeAsync(deploymentName, nodeTypeName);
            }

            public new async Task CreateClusterIpServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port)
            {
                await base.CreateClusterIpServiceAsync(appLabelValue, serviceNamePostfix, port);
            }

            public new async Task CreateLoadBalancerServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port, UInt16 targetPort)
            {
                await base.CreateLoadBalancerServiceAsync(appLabelValue, serviceNamePostfix, port, targetPort);
            }

            public new async Task UpdateServiceAsync(String serviceName, String appLabelValue)
            {
                await base.UpdateServiceAsync(serviceName, appLabelValue);
            }

            public new async Task DeleteServiceAsync(String name)
            {
                await base.DeleteServiceAsync(name);
            }

            public new async Task<IPAddress> GetLoadBalancerServiceIpAddressAsync(String serviceName)
            {
                return await base.GetLoadBalancerServiceIpAddressAsync(serviceName);
            }

            public new async Task CreateReaderNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
            {
                await base.CreateReaderNodeDeploymentAsync(name, persistentStorageCredentials, eventCacheServiceUrl);
            }

            public new async Task CreateEventCacheNodeDeploymentAsync(String name)
            {
                await base.CreateEventCacheNodeDeploymentAsync(name);
            }

            public new async Task CreateWriterNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials, Uri eventCacheServiceUrl)
            {
                await base.CreateWriterNodeDeploymentAsync(name, persistentStorageCredentials, eventCacheServiceUrl);
            }

            public new async Task CreateDistributedOperationCoordinatorNodeDeploymentAsync(String name, TestPersistentStorageLoginCredentials persistentStorageCredentials)
            {
                await base.CreateDistributedOperationCoordinatorNodeDeploymentAsync(name, persistentStorageCredentials);
            }

            public new async Task<IPAddress> CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(UInt16 port)
            {
                return await base.CreateDistributedOperationCoordinatorLoadBalancerServiceAsync(port);
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
                Boolean routingInitiallyOn
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
                    routingInitiallyOn
                );
            }

            public new async Task ScaleDeploymentAsync(String name, Int32 replicaCount)
            {
                await base.ScaleDeploymentAsync(name, replicaCount);
            }

            public new async Task WaitForLoadBalancerServiceAsync(String serviceName, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForLoadBalancerServiceAsync(serviceName, checkInterval, abortTimeout);
            }

            public new async Task WaitForDeploymentAvailabilityAsync(String name, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentAvailabilityAsync(name, checkInterval, abortTimeout);
            }

            public new async Task WaitForDeploymentScaleDownAsync(String name, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentScaleDownAsync(name, checkInterval, abortTimeout);
            }

            public new async Task WaitForDeploymentPredicateAsync(Predicate<V1Deployment> predicate, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.WaitForDeploymentPredicateAsync(predicate, checkInterval, abortTimeout);
            }

            public new async Task DeleteDeploymentAsync(String name)
            {
                await base.DeleteDeploymentAsync(name);
            }

            public new async Task ScaleDownAndDeleteDeploymentAsync(String name, Int32 checkInterval, Int32 abortTimeout)
            {
                await base.ScaleDownAndDeleteDeploymentAsync(name, checkInterval, abortTimeout);
            }

            #pragma warning restore 1591
        }

        #endregion
    }
}
