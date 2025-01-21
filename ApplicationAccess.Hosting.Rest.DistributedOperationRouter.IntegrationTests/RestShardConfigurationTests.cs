﻿/*
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
using ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.IntegrationTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models.RestShardConfiguration class.
    /// </summary>
    public class RestShardConfigurationTests
    {
        private RestShardConfiguration testRestShardConfiguration;

        [Test]
        public void Constructor_HashRangeEndLessThanHashRangeStart()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testRestShardConfiguration = new RestShardConfiguration("http", "127.0.0.1", 5000, 1000, 999);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'hashRangeEnd' with value 999 must be greater than or equal to the value 1000 of parameter 'hashRangeStart'."));
            Assert.AreEqual("hashRangeEnd", e.ParamName);
        }
    }
}
