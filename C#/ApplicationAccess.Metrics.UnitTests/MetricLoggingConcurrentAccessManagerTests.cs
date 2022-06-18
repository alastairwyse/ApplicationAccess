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
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.UnitTests;

namespace ApplicationAccess.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
    /// </summary>
    public class MetricLoggingConcurrentAccessManagerTests
    {
        private MetricLoggingConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testMetricLoggingConcurrentAccessManager;
        private IMetricLogger mockMetricLogger;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingConcurrentAccessManager = new MetricLoggingConcurrentAccessManager<string, string, ApplicationScreen, AccessLevel>(mockMetricLogger);
        }

        [Test]
        public void AddUser()
        {
            String testUser = "user1";

            testMetricLoggingConcurrentAccessManager.AddUser(testUser);

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).End(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<UsersAdded>());
            testMetricLoggingConcurrentAccessManager.Users.Contains(testUser);
        }

        [Test]
        public void AddUser_ExceptionWhenAdding()
        {
            String testUser = "user1";
            testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            mockMetricLogger.ClearReceivedCalls();

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testMetricLoggingConcurrentAccessManager.AddUser(testUser);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<UserAddTime>());
            mockMetricLogger.Received(1).CancelBegin(Arg.Any<UserAddTime>());
        }
    }
}
