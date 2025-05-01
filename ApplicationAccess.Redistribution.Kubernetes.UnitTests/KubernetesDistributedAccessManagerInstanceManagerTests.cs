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
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.LaunchPreparer;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
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
using NUnit.Framework.Internal;

// TODO: ** Remove these references and Project references **
using ApplicationAccess.Distribution.Persistence.SqlServer;
using ApplicationAccess.Redistribution.Persistence.SqlServer;
using ApplicationAccess.Persistence.Sql.SqlServer;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.KubernetesDistributedAccessManagerInstanceManager class.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerTests
    {
        protected String testNameSpace = "default";
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> emptyInstanceConfiguration;
        protected IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> mockPersistentStorageCreator;
        protected IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> mockAppSettingsConfigurer;
        protected IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer> mockShardConfigurationSetPersister;
        protected IKubernetesClientShim mockKubernetesClientShim;
        protected IApplicationLogger mockApplicationLogger;
        protected IMetricLogger mockMetricLogger;
        protected Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> testShardConfigurationSetPersisterCreationFunction;
        protected KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers testKubernetesDistributedAccessManagerInstanceManager;

        [SetUp]
        protected void SetUp()
        {
            testNameSpace = "default";
            emptyInstanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials>();
            mockPersistentStorageCreator = Substitute.For<IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials>>();
            mockAppSettingsConfigurer = Substitute.For<IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials>>();
            mockShardConfigurationSetPersister = Substitute.For<IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>>();
            mockKubernetesClientShim = Substitute.For<IKubernetesClientShim>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardConfigurationSetPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { return mockShardConfigurationSetPersister; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                emptyInstanceConfiguration, 
                mockPersistentStorageCreator,
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
                    mockPersistentStorageCreator,
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
                        1,
                        2,
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n2147483648"),
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
                    mockPersistentStorageCreator,
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
        public void Constructor_NextShardGroupIdCorrectlyUpdated()
        {
            var testInstanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials>
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.17:5000/"),
                WriterUrl = new Uri("http://10.104.198.18:5000/"),
                ShardConfigurationPersistentStorageCredentials = new("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=ApplicationAccessConfig"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        1,
                        2,
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                    )
                },
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        3,
                        4,
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=grouptogroup_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                    )
                },
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
                        5,
                        60,
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_n2147483648"),
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
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockApplicationLogger,
                mockMetricLogger
            );

            Assert.AreEqual(61, testKubernetesDistributedAccessManagerInstanceManager.NextShardGroupId);
        }

        [Test]
        public void CreateDistributedOperationRouterLoadBalancerService_ServiceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:5000/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            UInt16 port = 7001;

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerService(port);
            });

            Assert.That(e.Message, Does.StartWith($"A load balancer service for the distributed operation router has already been created."));
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerService_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerService_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerService_ExceptionRetrievingServiceIpAddress()
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for distributed router load balancer service 'operation-router-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationRouterLoadBalancerService()
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

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationRouterLoadBalancerService(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.DistributedOperationRouterUrl.ToString());
        }

        [Test]
        public void CreateWriterLoadBalancerService_ServiceAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                WriterUrl = new Uri("http://10.104.198.18:5000/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            UInt16 port = 7001;

            var e = Assert.ThrowsAsync<InvalidOperationException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerService(port);
            });

            Assert.That(e.Message, Does.StartWith($"A load balancer service for writer components has already been created."));
        }

        [Test]
        public async Task CreateWriterLoadBalancerService_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating writer load balancer service 'writer-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerService_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for writer load balancer service 'writer-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerService_ExceptionRetrievingServiceIpAddress()
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
                        Metadata = new V1ObjectMeta() { Name = "writer-externalservice" },
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for writer load balancer service 'writer-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateWriterLoadBalancerService()
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
                        Metadata = new V1ObjectMeta() { Name = "writer-externalservice" },
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

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterLoadBalancerService(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.AreEqual(IPAddress.Parse("10.104.198.18"), result);
            Assert.AreEqual("http://10.104.198.18:7001/", testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration.WriterUrl.ToString());
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

            Assert.That(e.Message, Does.StartWith($"A distributed operation router load balancer service must be created via method CreateDistributedOperationRouterLoadBalancerService() before creating a distributed AccessManager instance."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_WriterLoadBalancerServiceNotCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
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

            Assert.That(e.Message, Does.StartWith($"A writer load balancer service must be created via method CreateWriterLoadBalancerService() before creating a distributed AccessManager instance."));
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_UserShardGroupConfigurationAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                WriterUrl = new Uri("http://10.104.198.19:7001/"), 
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
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
        public void CreateDistributedAccessManagerInstanceAsync_GroupToGroupMappingShardGroupConfigurationAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                WriterUrl = new Uri("http://10.104.198.19:7001/"),
                GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
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
        public void CreateDistributedAccessManagerInstanceAsync_GroupShardGroupConfigurationAlreadyCreated()
        {
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = new()
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.18:7001/"),
                WriterUrl = new Uri("http://10.104.198.19:7001/"),
                GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
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
        public void CreateDistributedAccessManagerInstanceAsync_UserShardGroupConfigurationEmpty()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> 
                    { 
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'userShardGroupConfiguration' cannot be empty."));
            Assert.AreEqual("userShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupToGroupMappingShardGroupConfigurationEmpty()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (

                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>(),
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'groupToGroupMappingShardGroupConfiguration' must contain a single value (actually contained 0).  Only a single group to group mapping shard group is supported."));
            Assert.AreEqual("groupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupToGroupMappingShardGroupConfigurationContainsGreaterThan1Element()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'groupToGroupMappingShardGroupConfiguration' must contain a single value (actually contained 2).  Only a single group to group mapping shard group is supported."));
            Assert.AreEqual("groupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupShardGroupConfigurationEmpty()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'groupShardGroupConfiguration' cannot be empty."));
            Assert.AreEqual("groupShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_UserShardGroupConfigurationContainsDuplicateValues()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(10000)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'userShardGroupConfiguration' contains duplicate hash range start value 0."));
            Assert.AreEqual("userShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_UserShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(10000)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'userShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("userShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupToGroupMappingShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'groupToGroupMappingShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("groupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupShardGroupConfigurationContainsDuplicateValues()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(10000)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'groupShardGroupConfiguration' contains duplicate hash range start value 0."));
            Assert.AreEqual("groupShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void CreateDistributedAccessManagerInstanceAsync_GroupShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testKubernetesDistributedAccessManagerInstanceManager = CreateInstanceManagerWithLoadBalancerServices();

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue)
                    },
                    new List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                    {
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0),
                        new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(10000)
                    }
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'groupShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("groupShardGroupConfiguration", e.ParamName);
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
            mockPersistentStorageCreator.When((storageCreator) => storageCreator.CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName)).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedAccessManagerInstanceAsync
                (
                    userShardGroupConfiguration,
                    groupToGroupMappingShardGroupConfiguration,
                    groupShardGroupConfiguration
                );
            });

            mockPersistentStorageCreator.Received(1).CreateAccessManagerConfigurationPersistentStorage(persistentStorageInstanceName);
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
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_user_0").Returns(user0Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_715827884").Returns(group715827884Credentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));
            mockPersistentStorageCreator.CreateAccessManagerConfigurationPersistentStorage(configurationPersistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(configurationCredentials);
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
            mockPersistentStorageCreator.Received(1).CreateAccessManagerConfigurationPersistentStorage(configurationPersistentStorageInstanceName);
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(3, instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(5, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(7, instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(9, instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeId);
            Assert.AreEqual(11, instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeId);
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(0, capturedShardConfigurationList[0].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(1, capturedShardConfigurationList[1].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, capturedShardConfigurationList[2].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(3, capturedShardConfigurationList[3].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, capturedShardConfigurationList[4].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(5, capturedShardConfigurationList[5].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, capturedShardConfigurationList[6].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(7, capturedShardConfigurationList[7].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, capturedShardConfigurationList[8].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(9, capturedShardConfigurationList[9].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, capturedShardConfigurationList[10].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(11, capturedShardConfigurationList[11].Id);
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
                WriterUrl = new Uri("http://10.104.198.19:7001/"),
                ShardConfigurationPersistentStorageCredentials = new TestPersistentStorageLoginCredentials("alreadyPopulatedConnectionString")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                testInstanceConfiguration,
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_user_0").Returns(user0Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_715827884").Returns(group715827884Credentials);
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
            mockPersistentStorageCreator.DidNotReceive().CreateAccessManagerConfigurationPersistentStorage(Arg.Any<String>());
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(3, instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(5, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(7, instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(9, instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeId);
            Assert.AreEqual(11, instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeId);
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(0, capturedShardConfigurationList[0].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(1, capturedShardConfigurationList[1].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, capturedShardConfigurationList[2].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(3, capturedShardConfigurationList[3].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, capturedShardConfigurationList[4].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(5, capturedShardConfigurationList[5].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, capturedShardConfigurationList[6].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(7, capturedShardConfigurationList[7].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, capturedShardConfigurationList[8].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(9, capturedShardConfigurationList[9].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, capturedShardConfigurationList[10].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(11, capturedShardConfigurationList[11].Id);
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
                WriterUrl = new Uri("http://10.104.198.19:7001/"),
                ShardConfigurationPersistentStorageCredentials = new TestPersistentStorageLoginCredentials("alreadyPopulatedConnectionString")
            };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration() with { PersistentStorageInstanceNamePrefix = "" },
                testInstanceConfiguration,
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>()).Returns(testBeginId);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("user_n2147483648").Returns(userN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("user_0").Returns(user0Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("grouptogroupmapping_n2147483648").Returns(groupToGroupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("group_n2147483648").Returns(groupN2147483648Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("group_n715827882").Returns(groupN715827882Credentials);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage("group_715827884").Returns(group715827884Credentials);
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
            mockPersistentStorageCreator.DidNotReceive().CreateAccessManagerConfigurationPersistentStorage(Arg.Any<String>());
            mockShardConfigurationSetPersister.Received(1).Write(Arg.Any<ShardConfigurationSet<AccessManagerRestClientConfiguration>>(), true);
            mockMetricLogger.Received(1).Begin(Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<DistributedAccessManagerInstanceCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<DistributedAccessManagerInstanceCreated>());
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating distributed AccessManager instance in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating distributed AccessManager instance.");
            // Assertions on the instance shard configuration
            KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration = testKubernetesDistributedAccessManagerInstanceManager.InstanceConfiguration;
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration.Count);
            Assert.AreEqual(1, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration.Count);
            Assert.AreEqual(3, instanceConfiguration.GroupShardGroupConfiguration.Count);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(1, instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.UserShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(userN2147483648Credentials, instanceConfiguration.UserShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", instanceConfiguration.UserShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(3, instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(0, instanceConfiguration.UserShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(user0Credentials, instanceConfiguration.UserShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://user-reader-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://user-writer-0-service:5000/", instanceConfiguration.UserShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(5, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupToGroupN2147483648Credentials, instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", instanceConfiguration.GroupToGroupMappingShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeId);
            Assert.AreEqual(7, instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeId);
            Assert.AreEqual(Int32.MinValue, instanceConfiguration.GroupShardGroupConfiguration[0].HashRangeStart);
            Assert.AreEqual(groupN2147483648Credentials, instanceConfiguration.GroupShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[0].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeId);
            Assert.AreEqual(9, instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeId);
            Assert.AreEqual(-715_827_882, instanceConfiguration.GroupShardGroupConfiguration[1].HashRangeStart);
            Assert.AreEqual(groupN715827882Credentials, instanceConfiguration.GroupShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[1].WriterNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeId);
            Assert.AreEqual(11, instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeId);
            Assert.AreEqual(715_827_884, instanceConfiguration.GroupShardGroupConfiguration[2].HashRangeStart);
            Assert.AreEqual(group715827884Credentials, instanceConfiguration.GroupShardGroupConfiguration[2].PersistentStorageCredentials);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].ReaderNodeClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual("http://group-writer-715827884-service:5000/", instanceConfiguration.GroupShardGroupConfiguration[2].WriterNodeClientConfiguration.BaseUrl.ToString());
            // Assertions on the persisted shard configuration
            List<ShardConfiguration<AccessManagerRestClientConfiguration>> capturedShardConfigurationList = new(capturedShardConfigurationSet.Items);
            Assert.AreEqual(12, capturedShardConfigurationList.Count);
            Assert.AreEqual(0, capturedShardConfigurationList[0].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[0].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[0].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[0].HashRangeStart);
            Assert.AreEqual("http://user-reader-n2147483648-service:5000/", capturedShardConfigurationList[0].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(1, capturedShardConfigurationList[1].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[1].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[1].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[1].HashRangeStart);
            Assert.AreEqual("http://user-writer-n2147483648-service:5000/", capturedShardConfigurationList[1].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(2, capturedShardConfigurationList[2].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[2].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[2].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[2].HashRangeStart);
            Assert.AreEqual("http://user-reader-0-service:5000/", capturedShardConfigurationList[2].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(3, capturedShardConfigurationList[3].Id);
            Assert.AreEqual(DataElement.User, capturedShardConfigurationList[3].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[3].OperationType);
            Assert.AreEqual(0, capturedShardConfigurationList[3].HashRangeStart);
            Assert.AreEqual("http://user-writer-0-service:5000/", capturedShardConfigurationList[3].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(4, capturedShardConfigurationList[4].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[4].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[4].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[4].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-reader-n2147483648-service:5000/", capturedShardConfigurationList[4].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(5, capturedShardConfigurationList[5].Id);
            Assert.AreEqual(DataElement.GroupToGroupMapping, capturedShardConfigurationList[5].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[5].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[5].HashRangeStart);
            Assert.AreEqual("http://grouptogroupmapping-writer-n2147483648-service:5000/", capturedShardConfigurationList[5].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(6, capturedShardConfigurationList[6].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[6].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[6].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[6].HashRangeStart);
            Assert.AreEqual("http://group-reader-n2147483648-service:5000/", capturedShardConfigurationList[6].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(7, capturedShardConfigurationList[7].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[7].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[7].OperationType);
            Assert.AreEqual(Int32.MinValue, capturedShardConfigurationList[7].HashRangeStart);
            Assert.AreEqual("http://group-writer-n2147483648-service:5000/", capturedShardConfigurationList[7].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(8, capturedShardConfigurationList[8].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[8].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[8].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[8].HashRangeStart);
            Assert.AreEqual("http://group-reader-n715827882-service:5000/", capturedShardConfigurationList[8].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(9, capturedShardConfigurationList[9].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[9].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[9].OperationType);
            Assert.AreEqual(-715_827_882, capturedShardConfigurationList[9].HashRangeStart);
            Assert.AreEqual("http://group-writer-n715827882-service:5000/", capturedShardConfigurationList[9].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(10, capturedShardConfigurationList[10].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[10].DataElementType);
            Assert.AreEqual(Operation.Query, capturedShardConfigurationList[10].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[10].HashRangeStart);
            Assert.AreEqual("http://group-reader-715827884-service:5000/", capturedShardConfigurationList[10].ClientConfiguration.BaseUrl.ToString());
            Assert.AreEqual(11, capturedShardConfigurationList[11].Id);
            Assert.AreEqual(DataElement.Group, capturedShardConfigurationList[11].DataElementType);
            Assert.AreEqual(Operation.Event, capturedShardConfigurationList[11].OperationType);
            Assert.AreEqual(715_827_884, capturedShardConfigurationList[11].HashRangeStart);
            Assert.AreEqual("http://group-writer-715827884-service:5000/", capturedShardConfigurationList[11].ClientConfiguration.BaseUrl.ToString());
            // Assertions on the instance configuration distributed operation coordinator URL
            Assert.AreEqual("http://10.104.198.18:7000/", instanceConfiguration.DistributedOperationCoordinatorUrl.ToString());
        }

        [Test]
        public void ConstructShardConfigurationSetPersister_ShardConfigurationSetPersisterCreationFunctionInvokeFails()
        {
            var mockException = new Exception("Mock exception");
            testShardConfigurationSetPersisterCreationFunction = (TestPersistentStorageLoginCredentials credentials) => { throw mockException; };
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                emptyInstanceConfiguration, 
                mockPersistentStorageCreator,
                mockAppSettingsConfigurer,
                testShardConfigurationSetPersisterCreationFunction,
                mockKubernetesClientShim,
                mockApplicationLogger,
                mockMetricLogger
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testKubernetesDistributedAccessManagerInstanceManager.ConstructShardConfigurationSetPersister(new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password"));
            });

            Assert.That(e.Message, Does.StartWith($"Failed to construct ShardConfigurationSetPersister."));
            Assert.AreSame(mockException, e.InnerException);
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
                mockPersistentStorageCreator,
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
                mockPersistentStorageCreator,
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
        public void SortShardGroupConfigurationByHashRangeStart()
        {
            TestPersistentStorageLoginCredentials credentials1 = new("connString1");
            TestPersistentStorageLoginCredentials credentials2 = new("connString2");
            TestPersistentStorageLoginCredentials credentials3 = new("connString3");
            List<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> testShardGroupConfiguration = new()
            {
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MaxValue, credentials3),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(0, credentials2),
                new ShardGroupConfiguration<TestPersistentStorageLoginCredentials>(Int32.MinValue, credentials1),
            };

            testKubernetesDistributedAccessManagerInstanceManager.SortShardGroupConfigurationByHashRangeStart(testShardGroupConfiguration);

            Assert.AreEqual(Int32.MinValue, testShardGroupConfiguration[0].HashRangeStart);
            Assert.AreSame(credentials1, testShardGroupConfiguration[0].PersistentStorageCredentials);
            Assert.AreEqual(0, testShardGroupConfiguration[1].HashRangeStart);
            Assert.AreSame(credentials2, testShardGroupConfiguration[1].PersistentStorageCredentials);
            Assert.AreEqual(Int32.MaxValue, testShardGroupConfiguration[2].HashRangeStart);
            Assert.AreSame(credentials3, testShardGroupConfiguration[2].PersistentStorageCredentials);
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
            mockPersistentStorageCreator.When((storageCreator) => storageCreator.CreateAccessManagerPersistentStorage("applicationaccesstest_group_n10000")).Do((callInfo) => throw mockException);

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageCreator.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_group_n10000");
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
            TestPersistentStorageLoginCredentials storageCredentials = new(persistentStorageInstanceName);
            V1DeploymentList returnDeployments = new
            (
                new List<V1Deployment>{ new V1Deployment() { Metadata = new V1ObjectMeta() { Name = "grouptogroupmapping-eventcache-20000" }, Status = new V1DeploymentStatus { AvailableReplicas = 1 }  } }
            );
            mockMetricLogger.Begin(Arg.Any<ShardGroupCreateTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<PersistentStorageInstanceCreateTime>()).Returns(testBeginId2);
            mockMetricLogger.Begin(Arg.Any<EventCacheNodeCreateTime>()).Returns(testBeginId3);
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageCreator.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_20000");
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
            TestPersistentStorageLoginCredentials storageCredentials = new(persistentStorageInstanceName);
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
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-eventcache-20000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-reader-20000")), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("grouptogroupmapping-writer-20000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageCreator.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_grouptogroupmapping_20000");
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
            TestPersistentStorageLoginCredentials storageCredentials = new(persistentStorageInstanceName);
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
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-reader-n400000")), testNameSpace).Returns(Task.FromResult<V1Deployment>(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-writer-n400000")), testNameSpace).Returns(Task.FromException<V1Deployment>(mockException));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);
            });

            mockPersistentStorageCreator.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
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
            TestPersistentStorageLoginCredentials storageCredentials = new(persistentStorageInstanceName);
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
            mockPersistentStorageCreator.CreateAccessManagerPersistentStorage(persistentStorageInstanceName).Returns<TestPersistentStorageLoginCredentials>(storageCredentials);
            mockKubernetesClientShim.CreateNamespacedDeploymentAsync(null, Arg.Any<V1Deployment>(), testNameSpace).Returns(Task.FromResult(new V1Deployment()));
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult(new V1Service()));
            mockKubernetesClientShim.ListNamespacedDeploymentAsync(null, testNameSpace).Returns(Task.FromResult<V1DeploymentList>(returnDeployments));

            TestPersistentStorageLoginCredentials result = await testKubernetesDistributedAccessManagerInstanceManager.CreateShardGroupAsync(dataElement, hashRangeStart);

            Assert.AreEqual(storageCredentials, result);
            mockPersistentStorageCreator.Received(1).CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupCreated>());
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating shard group for data element 'User' and hash range start value -400000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'User' and hash range start value -400000...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating shard group.");
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
            TestPersistentStorageLoginCredentials storageCredentials = new(persistentStorageInstanceName);
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
            mockPersistentStorageCreator.DidNotReceive().CreateAccessManagerPersistentStorage("applicationaccesstest_user_n400000");
            await mockKubernetesClientShim.Received(1).CreateNamespacedDeploymentAsync(null, Arg.Is<V1Deployment>(DeploymentWithAppName("user-eventcache-n400000")), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.DidNotReceive().Begin(Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.DidNotReceive().End(testBeginId2, Arg.Any<PersistentStorageInstanceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<ShardGroupCreateTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<ShardGroupCreated>());
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating shard group for data element 'User' and hash range start value -400000 in namespace 'default'...");
            mockApplicationLogger.DidNotReceive().Log(ApplicationLogging.LogLevel.Information, "Creating persistent storage instance for data element 'User' and hash range start value -400000...");
            mockApplicationLogger.DidNotReceive().Log(ApplicationLogging.LogLevel.Information, "Completed creating persistent storage instance.");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating shard group.");
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Restarting shard group for data element 'User' and hash range start value -1 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed restarting shard group.");
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n3000000' to 0 replicas."));
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-writer-n3000000' to 0 replicas."));
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-eventcache-n3000000' to 0 replicas."));
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Scaling down shard group for data element 'User' and hash range start value -3000000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed scaling down shard group.");
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-eventcache-n3000000' to 1 replicas."));
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n3000000' to 1 replicas."));
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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-writer-n3000000' to 1 replicas."));
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Scaling up shard group for data element 'User' and hash range start value -3000000 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed scaling up shard group.");
        }

        [Test]
        public async Task CreateReaderNodeAsync_ExceptionCreatingNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("grouptogroupmapping_n2147483648");
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating reader node for data element 'GroupToGroupMapping' and hash range start value -2147483648 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating reader node.");
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating event cache node for data element 'User' and hash range start value -100 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating event cache node.");
        }

        [Test]
        public async Task CreateWriterNodeAsync_ExceptionCreatingNode()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n1");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("group_0");
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating writer node for data element 'Group' and hash range start value 0 in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating writer node.");
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorNodeAsync_ExceptionCreatingDeployment()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            TestPersistentStorageLoginCredentials storageCredentials = new("applicationaccess_shard_configuration");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("applicationaccess_shard_configuration");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("applicationaccess_shard_configuration");
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
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Creating distributed operation coordinator node in namespace 'default'...");
            mockApplicationLogger.Received(1).Log(ApplicationLogging.LogLevel.Information, "Completed creating distributed operation coordinator node.");
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, -2_147_483_648, createDeploymentFunction, "reader", 10_000);
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, -2_147_483_648, createDeploymentFunction, "reader", 10_000);
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, 2_147_483_647, createDeploymentFunction, "writer", 10_000);
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

            await testKubernetesDistributedAccessManagerInstanceManager.CreateApplicationAccessNodeAsync(deploymentName, -2_147_483_648, createDeploymentFunction, "event cache", 10_000);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedDeploymentAsync(null, testNameSpace);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerService_ExceptionCreatingService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromException<V1Service>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error creating distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerService_ExceptionWaitingForService()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            UInt16 port = 7001;
            mockMetricLogger.Begin(Arg.Any<LoadBalancerServiceCreateTime>()).Returns(testBeginId);
            mockKubernetesClientShim.CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace).Returns(Task.FromResult<V1Service>(new V1Service()));
            mockKubernetesClientShim.ListNamespacedServiceAsync(null, testNameSpace).Returns(Task.FromException<V1ServiceList>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(1).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Failed to wait for distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}' to become available."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerService_ExceptionRetrievingServiceIpAddress()
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
                await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerService(port);
            });

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
            Assert.That(e.Message, Does.StartWith($"Error retrieving IP address for distributed operation coordinator load balancer service 'operation-coordinator-externalservice' in namespace '{testNameSpace}'."));
            Assert.AreSame(mockException, e.InnerException.InnerException);
        }

        [Test]
        public async Task CreateDistributedOperationCoordinatorLoadBalancerService()
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

            IPAddress result = await testKubernetesDistributedAccessManagerInstanceManager.CreateDistributedOperationCoordinatorLoadBalancerService(port);

            await mockKubernetesClientShim.Received(1).CreateNamespacedServiceAsync(null, Arg.Any<V1Service>(), testNameSpace);
            await mockKubernetesClientShim.Received(2).ListNamespacedServiceAsync(null, testNameSpace);
            mockMetricLogger.Received(1).Begin(Arg.Any<LoadBalancerServiceCreateTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<LoadBalancerServiceCreateTime>());
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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateReaderNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for reader nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.ReaderNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateReaderNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
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
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventPersistence");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            Exception e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventPersistence' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("EventCacheConnection");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

            e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testKubernetesDistributedAccessManagerInstanceManager.CreateWriterNodeDeploymentAsync(name, storageCredentials, eventCacheServiceUrl);
            });

            Assert.That(e.Message, Does.StartWith("JSON path 'EventCacheConnection' was not found in JSON document containing appsettings configuration for writer nodes."));


            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            config = CreateStaticConfiguration();
            config.WriterNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("user_n2147483648");
            Uri eventCacheServiceUrl = new("http://user-eventcache-n2147483648-service:5000");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateWriterNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["EventPersistence"]["EventPersisterBackupFilePath"] = "/eventbackup/user-writer-n2147483648-eventbackup.json";
            expectedJsonConfiguration["EventCacheConnection"]["Host"] = eventCacheServiceUrl.ToString();
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
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
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
            testKubernetesDistributedAccessManagerInstanceManager.Dispose();
            KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration config = CreateStaticConfiguration();
            config.DistributedOperationCoordinatorNodeConfigurationTemplate.AppSettingsConfigurationTemplate.Remove("MetricLogging");
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
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
            TestPersistentStorageLoginCredentials storageCredentials = new("AccessManagerConfiguration");
            V1Deployment capturedDeploymentDefinition = null;
            JObject expectedJsonConfiguration = CreateDistributedOperationCoordinatorNodeAppSettingsConfigurationTemplate();
            expectedJsonConfiguration["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString;
            expectedJsonConfiguration["MetricLogging"]["MetricCategorySuffix"] = name;
            mockAppSettingsConfigurer.ConfigureAppsettingsJsonWithPersistentStorageCredentials(storageCredentials, Arg.Do<JObject>
            (
                appSettingsConfig => appSettingsConfig["AccessManagerSqlDatabaseConnection"]["ConnectionParameters"]["ConnectionString"] = storageCredentials.ConnectionString
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
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            testKubernetesDistributedAccessManagerInstanceManager = new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers(config, emptyInstanceConfiguration, mockPersistentStorageCreator, mockAppSettingsConfigurer, testShardConfigurationSetPersisterCreationFunction, mockKubernetesClientShim, mockApplicationLogger, mockMetricLogger);

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
            Assert.That(e.Message, Does.StartWith($"Failed to scale Kubernetes deployment 'user-reader-n2147483648' to 0 replicas."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ScaleDeploymentAsync()
        {
            String name = "user-reader-n2147483648";
            String expectedPatchContentString = $"{{\"spec\": {{\"replicas\": 3}}}}";
            V1Patch capturedPatchDefinition = null;
            await mockKubernetesClientShim.PatchNamespacedDeploymentScaleAsync(null, Arg.Do<V1Patch>(argumentValue => capturedPatchDefinition = argumentValue), name, testNameSpace);

            await testKubernetesDistributedAccessManagerInstanceManager.ScaleDeploymentAsync(name, 3);

            await mockKubernetesClientShim.Received(1).PatchNamespacedDeploymentScaleAsync(null, Arg.Any<V1Patch>(), name, testNameSpace);
            Assert.AreEqual(expectedPatchContentString, capturedPatchDefinition.Content);
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
        [Ignore("Integration test")]
        public async Task IntegrationTests_REMOVETHIS()
        {
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
                WriterUrl = new Uri("http://10.104.198.19:7001/")
            };
            return new KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                CreateStaticConfiguration(),
                instanceConfiguration,
                mockPersistentStorageCreator,
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
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 10,
                        ""RetryInterval"": 20,
                        ""OperationTimeout"": 0
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
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 500,
                        ""DequeueOperationLoopInterval"": 30000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
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
                    ""MetricLoggingEnabled"": false,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedBufferProcessor"",
                        ""BufferSizeLimit"": 501,
                        ""DequeueOperationLoopInterval"": 30001,
                        ""BufferProcessingFailureAction"": ""DisableMetricLogging""
                    },
                    ""MetricsSqlDatabaseConnection"": {
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
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 4,
                        ""RetryInterval"": 5,
                        ""OperationTimeout"": 120000
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
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 1000,
                        ""DequeueOperationLoopInterval"": 45000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
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
                ""AccessManagerSqlDatabaseConnection"": {
                    ""DatabaseType"": ""SqlServer"",
                    ""ConnectionParameters"": {
                        ""RetryCount"": 3,
                        ""RetryInterval"": 9,
                        ""OperationTimeout"": 120000
                    }
                },
                ""ShardConfigurationRefresh"": {
                    ""RefreshInterval"": ""11000""
                },
                ""ShardConnection"": {
                    ""RetryCount"": ""3"",
                    ""RetryInterval"": ""11"",
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 2000,
                        ""DequeueOperationLoopInterval"": 46000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
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
                },
                ""MetricLogging"": {
                    ""MetricLoggingEnabled"": true,
                    ""MetricBufferProcessing"": {
                        ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                        ""BufferSizeLimit"": 250,
                        ""DequeueOperationLoopInterval"": 2000,
                        ""BufferProcessingFailureAction"": ""ReturnServiceUnavailable""
                    },
                    ""MetricsSqlDatabaseConnection"": {
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
            /// <summary>The unique id to use for newly created shard groups.</summary>
            public Int32 NextShardGroupId 
            {
                get { return nextShardGroupId; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Static configuration for the instance manager (i.e. configuration which does not reflect the state of the distributed AccessManager instance).</param>
            /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> persistentStorageCreator,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Configuration for the instance manager.</param>
            /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
            /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration,
                IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> persistentStorageCreator,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, instanceConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, logger, metricLogger)
            {
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.UnitTests.KubernetesDistributedAccessManagerInstanceManagerTests+KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="staticConfiguration">Configuration for the instance manager.</param>
            /// <param name="instanceConfiguration">Configuration for an existing distributed AccessManager instance.</param>
            /// <param name="persistentStorageCreator">Used to create new instances of persistent storage used by the distributed AccessManager implementation.</param>
            /// <param name="credentialsAppSettingsConfigurer">Used to configure a component's 'appsettings.json' configuration with persistent storage credentials.</param>
            /// <param name="shardConfigurationSetPersister">Used to write shard configuration to persistent storage.</param>
            /// <param name="kubernetesClientShim">A mock <see cref="IKubernetesClientShim"/>.</param>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public KubernetesDistributedAccessManagerInstanceManagerWithProtectedMembers
            (
                KubernetesDistributedAccessManagerInstanceManagerStaticConfiguration staticConfiguration,
                KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> instanceConfiguration,
                IDistributedAccessManagerPersistentStorageCreator<TestPersistentStorageLoginCredentials> persistentStorageCreator,
                IPersistentStorageCredentialsAppSettingsConfigurer<TestPersistentStorageLoginCredentials> credentialsAppSettingsConfigurer,
                Func<TestPersistentStorageLoginCredentials, IShardConfigurationSetPersister<AccessManagerRestClientConfiguration, AccessManagerRestClientConfigurationJsonSerializer>> shardConfigurationSetPersisterCreationFunction,
                IKubernetesClientShim kubernetesClientShim,
                IApplicationLogger logger,
                IMetricLogger metricLogger
            )
                : base(staticConfiguration, instanceConfiguration, persistentStorageCreator, credentialsAppSettingsConfigurer, shardConfigurationSetPersisterCreationFunction, kubernetesClientShim, logger, metricLogger)
            {
            }

            #pragma warning disable 1591

            public new void ConstructShardConfigurationSetPersister(TestPersistentStorageLoginCredentials persistentStorageCredentials)
            {
                base.ConstructShardConfigurationSetPersister(persistentStorageCredentials);
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

            public new void SortShardGroupConfigurationByHashRangeStart(IList<ShardGroupConfiguration<TestPersistentStorageLoginCredentials>> shardGroupConfiguration)
            {
                base.SortShardGroupConfigurationByHashRangeStart(shardGroupConfiguration);
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

            public new async Task CreateApplicationAccessNodeAsync(String deploymentName, Int32 hashRangeStart, Func<Task> createDeploymentFunction, String nodeTypeName, Int32 abortTimeout)
            {
                await base.CreateApplicationAccessNodeAsync(deploymentName, hashRangeStart, createDeploymentFunction, nodeTypeName, abortTimeout);
            }

            public new async Task CreateClusterIpServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port)
            {
                await base.CreateClusterIpServiceAsync(appLabelValue, serviceNamePostfix, port);
            }

            public new async Task CreateLoadBalancerServiceAsync(String appLabelValue, String serviceNamePostfix, UInt16 port, UInt16 targetPort)
            {
                await base.CreateLoadBalancerServiceAsync(appLabelValue, serviceNamePostfix, port, targetPort);
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

            public new async Task<IPAddress> CreateDistributedOperationCoordinatorLoadBalancerService(UInt16 port)
            {
                return await base.CreateDistributedOperationCoordinatorLoadBalancerService(port);
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

            #pragma warning restore 1591
        }

        #endregion
    }
}
