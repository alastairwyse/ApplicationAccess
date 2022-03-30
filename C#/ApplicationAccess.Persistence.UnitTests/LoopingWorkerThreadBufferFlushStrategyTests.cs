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
using ApplicationAccess.UnitTests;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
    /// </summary>
    public class LoopingWorkerThreadBufferFlushStrategyTests
    {
        private LoopingWorkerThreadBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel> testLoopingWorkerThreadBufferFlushStrategy;

        [SetUp]
        protected void SetUp()
        {
            testLoopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel>(500);
        }

        [TearDown]
        protected void TearDown()
        {
            testLoopingWorkerThreadBufferFlushStrategy.Dispose();
        }

        [Test]
        public void Constructor_FlushLoopIntervalParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testLoopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel>(0);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'flushLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual(e.ParamName, "flushLoopInterval");
        }

    }
}
