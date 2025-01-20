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
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using NUnit.Framework;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.ShardConfiguration class.
    /// </summary>
    public class ShardConfigurationTests
    {
        [Test]
        public void Describe()
        {
            var testShardConfiguration = new ShardConfiguration<AccessManagerRestClientConfiguration>(DataElement.User, Operation.Query, 16, new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.0.1:5001")));

            String result = testShardConfiguration.Describe(true);

            Assert.AreEqual($"DataElementType = User, OperationType = Query, HashRangeStart = 16, ClientConfiguration = AccessManagerRestClientConfiguration {{ BaseUrl = http://127.0.0.0.1:5001/ }}", result);


            result = testShardConfiguration.Describe(false);

            Assert.AreEqual($"DataElementType = User, OperationType = Query, ClientConfiguration = AccessManagerRestClientConfiguration {{ BaseUrl = http://127.0.0.0.1:5001/ }}", result);
        }
    }
}
