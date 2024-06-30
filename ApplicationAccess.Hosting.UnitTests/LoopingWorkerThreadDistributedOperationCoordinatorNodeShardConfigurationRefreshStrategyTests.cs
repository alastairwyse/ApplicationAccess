/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Threading;
using ApplicationAccess.Distribution;
using ApplicationAccess.Persistence;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy class.
    /// </summary>
    public class LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategyTests
    {
        // TODO: Tests for some functionality (e.g. Stop() method) are not included here, as the class is copied from
        //   LoopingWorkerThreadReaderNodeRefreshStrategy, and that functionality is covered in tests for that class

        private LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy testLoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy;

        [SetUp]
        protected void SetUp()
        {
            testLoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy = new LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy(250, (ShardConfigurationRefreshException shardConfigurationRefreshException) => { });
        }

        [Test]
        public void Constructor_RefreshLoopIntervalLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testLoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy = new LoopingWorkerThreadDistributedOperationCoordinatorNodeShardConfigurationRefreshStrategy(0, (ShardConfigurationRefreshException shardConfigurationRefreshException) => { });
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'refreshLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual("refreshLoopInterval", e.ParamName);
        }
    }
}
