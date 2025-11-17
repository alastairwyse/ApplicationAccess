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
using ApplicationAccess;
using ApplicationAccess.Hosting;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Persistence;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Factories.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Factories.AccessManagerEventCacheClientFactory class.
    /// </summary>
    public class AccessManagerEventCacheClientFactoryTests
    {
        protected IApplicationLogger testLogger;
        protected IMetricLogger testMetricLogger;
        protected String testHost = "http://127.0.0.1:5000/";
        private AccessManagerEventCacheClientFactory<String, String, String, String> testAccessManagerEventCacheClientFactory;

        [SetUp]
        protected void SetUp()
        {
            testLogger = new NullLogger();
            testMetricLogger = Substitute.For<IMetricLogger>();
            testAccessManagerEventCacheClientFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger,
                testMetricLogger
            );
        }

        [Test]
        public void GetClient_RestClientWithoutMetrics()
        {
            EventCacheConnectionOptions testEventCacheConnectionOptions = new()
            {
                Protocol = Protocol.Rest, 
                Host = testHost, 
                RetryCount = 5, 
                RetryInterval = 10
            };
            testAccessManagerEventCacheClientFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerEventCache<String, String, String, String> result = testAccessManagerEventCacheClientFactory.GetClient(testEventCacheConnectionOptions))
            {

                Assert.IsAssignableFrom<Rest.Client.EventCacheClient<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "logger" }, testLogger, result, true);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, result);
            }
        }

        [Test]
        public void GetClient_RestClientWithMetrics()
        {
            EventCacheConnectionOptions testEventCacheConnectionOptions = new()
            {
                Protocol = Protocol.Rest,
                Host = testHost,
                RetryCount = 5,
                RetryInterval = 10
            };

            using (IAccessManagerEventCache<String, String, String, String> result = testAccessManagerEventCacheClientFactory.GetClient(testEventCacheConnectionOptions))
            {

                Assert.IsAssignableFrom<Rest.Client.EventCacheClient<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "logger" }, testLogger, result, true);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, result, true);
            }
        }

        [Test]
        public void GetClient_GrpcClientWithoutMetrics()
        {
            EventCacheConnectionOptions testEventCacheConnectionOptions = new()
            {
                Protocol = Protocol.Grpc,
                Host = testHost,
                RetryCount = 5,
                RetryInterval = 10
            };
            testAccessManagerEventCacheClientFactory = new
            (
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                testLogger
            );

            using (IAccessManagerEventCache<String, String, String, String> result = testAccessManagerEventCacheClientFactory.GetClient(testEventCacheConnectionOptions))
            {

                Assert.IsAssignableFrom<Grpc.Client.EventCacheClient<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "logger" }, testLogger, result, true);
                NonPublicFieldAssert.IsOfType<NullMetricLogger>(new List<String>() { "metricLogger" }, result);
            }
        }

        [Test]
        public void GetClient_GrpcClientWithMetrics()
        {
            EventCacheConnectionOptions testEventCacheConnectionOptions = new()
            {
                Protocol = Protocol.Grpc,
                Host = testHost,
                RetryCount = 5,
                RetryInterval = 10
            };

            using (IAccessManagerEventCache<String, String, String, String> result = testAccessManagerEventCacheClientFactory.GetClient(testEventCacheConnectionOptions))
            {

                Assert.IsAssignableFrom<Grpc.Client.EventCacheClient<String, String, String, String>>(result);
                NonPublicFieldAssert.HasValue(new List<String>() { "logger" }, testLogger, result, true);
                NonPublicFieldAssert.HasValue(new List<String>() { "metricLogger" }, testMetricLogger, result, true);
            }
        }
    }
}
