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

using ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models;
using NUnit.Framework;
using System;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.IntegrationTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models.RouteConfiguration class.
    /// </summary>
    public class RouteConfigurationTests
    {
        private RouteConfiguration testRouteConfiguration;

        [Test]
        public void Constructor_TargetShardConfigurationHashRangeStartNotContiguousWithSourceShardConfigurationHashRangeEnd()
        {
            var sourceShardConfiguration = new RestShardConfiguration("http", "127.0.0.1", 5000, 0, 999);
            var targetShardConfiguration = new RestShardConfiguration("http", "127.0.0.1", 5001, 1001, 2000);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testRouteConfiguration = new RouteConfiguration(sourceShardConfiguration, targetShardConfiguration);
            });

            Assert.That(e.Message, Does.StartWith("Property 'HashRangeStart' of parameter 'targetShardConfiguration' with value 1001 must be contiguous with property 'HashRangeEnd' of parameter 'sourceShardConfiguration' with value 999."));
            Assert.AreEqual("targetShardConfiguration", e.ParamName);
        }
    }
}
