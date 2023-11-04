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
using NUnit.Framework;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.ShardConfigurationSet class.
    /// </summary>
    public class ShardConfigurationSetTests
    {
        [Test]
        public void Constructor_ConfigurationItemsParameterContainsDuplicateItems()
        {
            var testConfigurationItems = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6000")),
                // Below item has same keys as 2nd item
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://localhost:5000"))
            };

            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testShardConfigurationSet = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'configurationItems' contains duplicate items with DataElementType 'User', OperationType 'Query', and HashRangeStart 16."));
            Assert.AreEqual("configurationItems", e.ParamName);
        }

        [Test]
        public void Equals_DifferingCounts()
        {
            var testConfigurationItems1 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000"))
            };
            var testConfigurationItems2 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000"))
            };
            var shardConfigurationSet1 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems1);
            var shardConfigurationSet2 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems2);

            Boolean result = shardConfigurationSet1.Equals(shardConfigurationSet2);

            Assert.IsFalse(result);


            result = shardConfigurationSet2.Equals(shardConfigurationSet1);

            Assert.IsFalse(result);
        }

        [Test]
        public void Equals_SameCountsDifferingContents()
        {
            var testConfigurationItems1 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000"))
            };
            var testConfigurationItems2 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                // Port in below URL is different
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6999")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000"))
            };
            var shardConfigurationSet1 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems1);
            var shardConfigurationSet2 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems2);

            Boolean result = shardConfigurationSet1.Equals(shardConfigurationSet2);

            Assert.IsFalse(result);


            result = shardConfigurationSet2.Equals(shardConfigurationSet1);

            Assert.IsFalse(result);
        }

        [Test]
        public void Equals()
        {
            var testConfigurationItems1 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000"))
            };
            var testConfigurationItems2 = new List<ShardConfiguration<AccessManagerRestClientConfiguration>>()
            {
                // Same data as 'testConfigurationItems1' but in different order
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Event, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:7000")), 
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5001")), 
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:5000")),
                new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.Group, Operation.Query, 0, new AccessManagerRestClientConfiguration("http://127.0.0.0.1:6000"))
            };
            var shardConfigurationSet1 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems1);
            var shardConfigurationSet2 = new ShardConfigurationSet<AccessManagerRestClientConfiguration>(testConfigurationItems2);

            Boolean result = shardConfigurationSet1.Equals(shardConfigurationSet2);

            Assert.IsTrue(result);


            result = shardConfigurationSet2.Equals(shardConfigurationSet1);

            Assert.IsTrue(result);
        }
    }
}
