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
using ApplicationAccess.Redistribution.Kubernetes.Models;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Redistribution.Kubernetes.UnitTests.Models
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.Kubernetes.Models.KubernetesShardGroupConfigurationSet class.
    /// </summary>
    public class KubernetesShardGroupConfigurationSetTests
    {
        private List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> testItems;
        private KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials> testKubernetesShardGroupConfigurationSet;

        [SetUp]
        protected void SetUp()
        {
            testItems = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MaxValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_2147483647"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-2147483647:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-2147483647:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MinValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n2147483648"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n2147483648:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n2147483648:5000/"))
                )
            };
            testKubernetesShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials>(testItems);
        }

        [Test]
        public void Constructor_ItemsParameterContainsDuplicates()
        {
            testItems = new List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>()
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    Int32.MaxValue,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_2147483647"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-2147483647:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-2147483647:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials>(testItems);
            });

            Assert.That(e.Message, Does.StartWith($"Failed to add all items in parameter 'items' to the set."));
            Assert.That(e.InnerException.Message, Does.StartWith($"An item with hash range start value 0 specified in parameter 'newItem' already exists in the set."));
            Assert.AreEqual("items", e.ParamName);
        }

        [Test]
        public void Count()
        {
            Assert.AreEqual(3, testKubernetesShardGroupConfigurationSet.Count);


            testKubernetesShardGroupConfigurationSet = new KubernetesShardGroupConfigurationSet<TestPersistentStorageLoginCredentials>();

            Assert.AreEqual(0, testKubernetesShardGroupConfigurationSet.Count);
        }

        [Test]
        public void Items()
        {
            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;

            Assert.AreEqual(3, result.Count);
            Assert.AreSame(testItems[0], result[2]);
            Assert.AreSame(testItems[1], result[1]);
            Assert.AreSame(testItems[2], result[0]);
        }

        [Test]
        public void Clear()
        {
            testKubernetesShardGroupConfigurationSet.Clear();

            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void Add_ItemWithHashRangeStartAlreadyExists()
        {
            var newItem = new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
            (
                Int32.MaxValue,
                new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_2147483647"),
                new AccessManagerRestClientConfiguration(new Uri("http://user-reader-2147483647:5000/")),
                new AccessManagerRestClientConfiguration(new Uri("http://user-writer-2147483647:5000/"))
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesShardGroupConfigurationSet.Add(newItem);
            });

            Assert.That(e.Message, Does.StartWith($"An item with hash range start value 2147483647 specified in parameter 'newItem' already exists in the set."));
            Assert.AreEqual("newItem", e.ParamName);
        }

        [Test]
        public void Add()
        {
            var newItem = new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
            (
                1073741824,
                new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_1073741824"),
                new AccessManagerRestClientConfiguration(new Uri("http://user-reader-1073741824:5000/")),
                new AccessManagerRestClientConfiguration(new Uri("http://user-writer-1073741824:5000/"))
            );

            testKubernetesShardGroupConfigurationSet.Add(newItem);

            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;
            Assert.AreEqual(4, result.Count);
            Assert.AreSame(testItems[0], result[3]);
            Assert.AreSame(newItem, result[2]);
            Assert.AreSame(testItems[1], result[1]);
            Assert.AreSame(testItems[2], result[0]);
        }

        [Test]
        public void AddRange_ParameterNewItemsContainsDuplicateHashRangeStart()
        {
            List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> newItems = new()
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    1073741824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_1073741824"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-1073741824:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-1073741824:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    0,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_0"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-0:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-0:5000/"))
                )
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesShardGroupConfigurationSet.AddRange(newItems);
            });

            Assert.That(e.Message, Does.StartWith($"An item with hash range start value 0 specified in an item of parameter 'newItems' already exists in the set."));
            Assert.AreEqual("newItems", e.ParamName);
        }

        [Test]
        public void AddRange()
        {
            List<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>>  newItems = new()
            {
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    1073741824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_1073741824"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-1073741824:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-1073741824:5000/"))
                ),
                new KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>
                (
                    -1073741824,
                    new TestPersistentStorageLoginCredentials("Server=127.0.0.1;User Id=sa;Password=password;InitialCatalog=user_n1073741824"),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-reader-n1073741824:5000/")),
                    new AccessManagerRestClientConfiguration(new Uri("http://user-writer-n1073741824:5000/"))
                )
            };

            testKubernetesShardGroupConfigurationSet.AddRange(newItems);

            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;
            Assert.AreEqual(5, result.Count);
            Assert.AreSame(testItems[0], result[4]);
            Assert.AreSame(newItems[0], result[3]);
            Assert.AreSame(testItems[1], result[2]);
            Assert.AreSame(newItems[1], result[1]);
            Assert.AreSame(testItems[2], result[0]);
        }

        [Test]
        public void Remove_ParameterItemHashRangeStartContainsInvalidValue()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesShardGroupConfigurationSet.Remove(-1);
            });

            Assert.That(e.Message, Does.StartWith($"No item with hash range start value -1 specified in parameter 'itemHashRangeStart' exists in the set."));
            Assert.AreEqual("itemHashRangeStart", e.ParamName);
        }

        [Test]
        public void Remove()
        {
            testKubernetesShardGroupConfigurationSet.Remove(0);

            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;
            Assert.AreEqual(2, result.Count);
            Assert.AreSame(testItems[0], result[1]);
            Assert.AreSame(testItems[2], result[0]);
        }

        [Test]
        public void ContainsHashRangeStart()
        {
            Assert.IsTrue(testKubernetesShardGroupConfigurationSet.ContainsHashRangeStart(Int32.MinValue));
            Assert.IsTrue(testKubernetesShardGroupConfigurationSet.ContainsHashRangeStart(0));
            Assert.IsTrue(testKubernetesShardGroupConfigurationSet.ContainsHashRangeStart(Int32.MaxValue));
            Assert.IsFalse(testKubernetesShardGroupConfigurationSet.ContainsHashRangeStart(-1));
            Assert.IsFalse(testKubernetesShardGroupConfigurationSet.ContainsHashRangeStart(1073741824));
        }

        [Test]
        public void UpdateRestClientConfiguration_ParameterItemHashRangeStartContainsInvalidValue()
        {
            var readerNodeClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://operation-router:5000/"));
            var writerNodeClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://operation-router:5000/"));

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testKubernetesShardGroupConfigurationSet.UpdateRestClientConfiguration(-1, readerNodeClientConfiguration, writerNodeClientConfiguration);
            });

            Assert.That(e.Message, Does.StartWith($"No item with hash range start value -1 specified in parameter 'itemHashRangeStart' exists in the set."));
            Assert.AreEqual("itemHashRangeStart", e.ParamName);
        }

        [Test]
        public void UpdateRestClientConfiguration()
        {
            var readerNodeClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://operation-router:5000/"));
            var writerNodeClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://operation-router:5000/"));

            testKubernetesShardGroupConfigurationSet.UpdateRestClientConfiguration(Int32.MinValue, readerNodeClientConfiguration, writerNodeClientConfiguration);

            IList<KubernetesShardGroupConfiguration<TestPersistentStorageLoginCredentials>> result = testKubernetesShardGroupConfigurationSet.Items;
            Assert.AreSame(readerNodeClientConfiguration, result[0].ReaderNodeClientConfiguration);
            Assert.AreSame(writerNodeClientConfiguration, result[0].WriterNodeClientConfiguration);
        }
    }
}
