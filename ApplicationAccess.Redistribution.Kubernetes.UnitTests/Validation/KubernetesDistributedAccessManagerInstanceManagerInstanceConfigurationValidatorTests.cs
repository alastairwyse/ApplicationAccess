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
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Kubernetes.Models;
using ApplicationAccess.Redistribution.Kubernetes.Validation;
using ApplicationAccess.Redistribution.Models;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests.Validation
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Validation.KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidator class.
    /// </summary>
    public class KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidatorTests
    {
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials> testInstanceConfiguration;
        protected KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidator<TestPersistentStorageLoginCredentials> testInstanceConfigurationValidator;

        [SetUp]
        protected void SetUp()
        {
            testInstanceConfiguration = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration<TestPersistentStorageLoginCredentials>
            {
                DistributedOperationRouterUrl = new Uri("http://10.104.198.17:5000/"),
                Writer1Url = new Uri("http://10.104.198.18:5000/"),
                Writer2Url = new Uri("http://10.104.198.20:5000/"),
                ShardConfigurationPersistentStorageCredentials = new ("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=ApplicationAccessConfig"),
                UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
                {
                    new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                    (
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
                        Int32.MinValue,
                        new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_n2147483648"),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                        new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                    )
                },
                DistributedOperationCoordinatorUrl = new Uri("http://10.104.198.19:7000/")
            };
            testInstanceConfigurationValidator = new KubernetesDistributedAccessManagerInstanceManagerInstanceConfigurationValidator<TestPersistentStorageLoginCredentials>();
        }

        [Test]
        public void Validate_ShardGroupConfigurationPropertiesNotAllNullOrNotNull()
        {
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = null;

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration '*ShardGroupConfiguration' properties must be either all null or all non-null."));
        }

        [Test]
        public void Validate_UserShardGroupConfigurationPropertyEmpty()
        {
            testInstanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'UserShardGroupConfiguration' cannot be empty."));
            Assert.AreEqual("UserShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupToGroupMappingShardGroupConfigurationPropertyEmpty()
        {
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'GroupToGroupMappingShardGroupConfiguration' must contain a single value (actually contained 0).  Only a single group to group mapping shard group is supported."));
            Assert.AreEqual("GroupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupToGroupMappingShardGroupConfigurationPropertyContainsGreaterThan1Element()
        {
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=grouptogroup_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=grouptogroup_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-0-service:5000/"))
                ),
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'GroupToGroupMappingShardGroupConfiguration' must contain a single value (actually contained 2).  Only a single group to group mapping shard group is supported."));
            Assert.AreEqual("GroupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupShardGroupConfigurationPropertyEmpty()
        {
            testInstanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'GroupShardGroupConfiguration' cannot be empty."));
            Assert.AreEqual("GroupShardGroupConfiguration", e.ParamName);
        }
        [Test]
        public void Validate_UserShardGroupConfigurationContainsDuplicateValues()
        {
            testInstanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648-service:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'UserShardGroupConfiguration' contains duplicate hash range start value {Int32.MinValue}."));
            Assert.AreEqual("UserShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_UserShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testInstanceConfiguration.UserShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    3,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_3"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-3-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-3-service:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'UserShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("UserShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupToGroupMappingShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=grouptogroup_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-reader-0-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://grouptogroup-writer-0-service:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'GroupToGroupMappingShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("GroupToGroupMappingShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupShardGroupConfigurationContainsDuplicateValues()
        {
            testInstanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-n2147483648-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-n2147483648-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    1,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_1"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-1-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-1-service:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    1,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_1"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-1-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-1-service:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'GroupShardGroupConfiguration' contains duplicate hash range start value 1."));
            Assert.AreEqual("GroupShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_GroupShardGroupConfigurationDoesntContainInt32MinValue()
        {
            testInstanceConfiguration.GroupShardGroupConfiguration = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    2,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=group_2"),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-reader-2-service:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://group-writer-2-service:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"Property or parameter 'GroupShardGroupConfiguration' must contain one element with value {Int32.MinValue}."));
            Assert.AreEqual("GroupShardGroupConfiguration", e.ParamName);
        }

        [Test]
        public void Validate_DistributedOperationCoordinatorUrlPropertyNotNullWhenShardGroupConfigurationPropertiesAreNull()
        {
            testInstanceConfiguration.UserShardGroupConfiguration = null;
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = null;
            testInstanceConfiguration.GroupShardGroupConfiguration = null;

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration 'DistributedOperationCoordinatorUrl' property must null when the '*ShardGroupConfiguration' properties are null."));
            Assert.AreEqual("DistributedOperationCoordinatorUrl", e.ParamName);
        }

        [Test]
        public void Validate_DistributedOperationRouterUrlPropertyNullWhenShardGroupConfigurationPropertiesNotNull()
        {
            testInstanceConfiguration.DistributedOperationRouterUrl = null;

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'DistributedOperationRouterUrl' cannot be null when the '*ShardGroupConfiguration' properties are non-null."));
            Assert.AreEqual("DistributedOperationRouterUrl", e.ParamName);
        }

        [Test]
        public void Validate_Writer1UrlPropertyNullWhenShardGroupConfigurationPropertiesNotNull()
        {
            testInstanceConfiguration.Writer1Url = null;

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'Writer1Url' cannot be null when the '*ShardGroupConfiguration' properties are non-null."));
            Assert.AreEqual("Writer1Url", e.ParamName);
        }

        [Test]
        public void Validate_Writer2UrlPropertyNullWhenShardGroupConfigurationPropertiesNotNull()
        {
            testInstanceConfiguration.Writer2Url = null;

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'Writer2Url' cannot be null when the '*ShardGroupConfiguration' properties are non-null."));
            Assert.AreEqual("Writer2Url", e.ParamName);
        }

        [Test]
        public void Validate_ShardConfigurationPersistentStorageCredentialsPropertyNullWhenShardGroupConfigurationPropertiesNotNull()
        {
            testInstanceConfiguration.ShardConfigurationPersistentStorageCredentials = null;

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'ShardConfigurationPersistentStorageCredentials' cannot be null when the '*ShardGroupConfiguration' properties are non-null."));
            Assert.AreEqual("ShardConfigurationPersistentStorageCredentials", e.ParamName);
        }

        [Test]
        public void Validate_DistributedOperationCoordinatorUrlPropertyNullWhenShardGroupConfigurationPropertiesNotNull()
        {
            testInstanceConfiguration.DistributedOperationCoordinatorUrl = null;

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"KubernetesDistributedAccessManagerInstanceManagerInstanceConfiguration property 'DistributedOperationCoordinatorUrl' cannot be null when the '*ShardGroupConfiguration' properties are non-null."));
            Assert.AreEqual("DistributedOperationCoordinatorUrl", e.ParamName);
        }

        [Test]
        public void Validate()
        {
            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.UserShardGroupConfiguration = null;
            testInstanceConfiguration.GroupToGroupMappingShardGroupConfiguration = null;
            testInstanceConfiguration.GroupShardGroupConfiguration = null;
            testInstanceConfiguration.DistributedOperationCoordinatorUrl = null;

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.ShardConfigurationPersistentStorageCredentials = null;

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.Writer2Url = null;

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.Writer1Url = null;

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.Writer2Url = new Uri("http://10.104.198.20:5000/");

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);


            testInstanceConfiguration.Writer2Url = null;
            testInstanceConfiguration.DistributedOperationRouterUrl = null;

            testInstanceConfigurationValidator.Validate(testInstanceConfiguration);
        }
    }
}
