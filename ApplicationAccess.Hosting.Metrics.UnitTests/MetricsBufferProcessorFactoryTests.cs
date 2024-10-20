/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.UnitTests;
using ApplicationLogging;
using ApplicationMetrics.MetricLoggers;
using ApplicationMetrics.MetricLoggers.SqlServer;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Metrics.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Persistence.Sql.MetricsBufferProcessorFactory class.
    /// </summary>
    public class MetricsBufferProcessorFactoryTests
    {
        protected MetricsBufferProcessorFactory testMetricsBufferProcessorFactory;

        [SetUp]
        protected void SetUp()
        {
            testMetricsBufferProcessorFactory = new MetricsBufferProcessorFactory();
        }

        [Test]
        public void GetBufferProcessor_SizeLimitedBufferProcessor()
        {
            var testBufferProcessingOptions = new MetricBufferProcessingOptions();
            testBufferProcessingOptions.BufferProcessingStrategy = MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor;
            testBufferProcessingOptions.BufferSizeLimit = 50;
            testBufferProcessingOptions.DequeueOperationLoopInterval = 10000;
            testBufferProcessingOptions.BufferProcessingFailureAction = MetricBufferProcessingFailureAction.DisableMetricLogging;
            Action<Exception> testBufferProcessingExceptionAction = (Exception bufferProcessingExceptionAction) => { Console.WriteLine(bufferProcessingExceptionAction.Message); };

            using (WorkerThreadBufferProcessorBase testBufferProcessor = testMetricsBufferProcessorFactory.GetBufferProcessor(testBufferProcessingOptions, testBufferProcessingExceptionAction, false))
            {

                Assert.IsAssignableFrom<SizeLimitedBufferProcessor>(testBufferProcessor);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "bufferSizeLimit" }, 50, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Action<Exception>>(new List<String>() { "bufferProcessingExceptionAction" }, testBufferProcessingExceptionAction, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Boolean>(new List<String>() { "rethrowBufferProcessingException" }, false, testBufferProcessor);
            }
        }

        [Test]
        public void GetBufferProcessor_LoopingWorkerThreadBufferProcessor()
        {
            var testBufferProcessingOptions = new MetricBufferProcessingOptions();
            testBufferProcessingOptions.BufferProcessingStrategy = MetricBufferProcessingStrategyImplementation.LoopingWorkerThreadBufferProcessor;
            testBufferProcessingOptions.BufferSizeLimit = 75;
            testBufferProcessingOptions.DequeueOperationLoopInterval = 15000;
            testBufferProcessingOptions.BufferProcessingFailureAction = MetricBufferProcessingFailureAction.ReturnServiceUnavailable;
            Action<Exception> testBufferProcessingExceptionAction = (Exception bufferProcessingExceptionAction) => { Console.WriteLine(bufferProcessingExceptionAction.Message); };

            using (WorkerThreadBufferProcessorBase testBufferProcessor = testMetricsBufferProcessorFactory.GetBufferProcessor(testBufferProcessingOptions, testBufferProcessingExceptionAction, true))
            {

                Assert.IsAssignableFrom<LoopingWorkerThreadBufferProcessor>(testBufferProcessor);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "dequeueOperationLoopInterval" }, 15000, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Action<Exception>>(new List<String>() { "bufferProcessingExceptionAction" }, testBufferProcessingExceptionAction, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Boolean>(new List<String>() { "rethrowBufferProcessingException" }, true, testBufferProcessor);
            }
        }

        [Test]
        public void GetBufferProcessor_SizeLimitedLoopingWorkerThreadHybridBufferProcessor()
        {
            var testBufferProcessingOptions = new MetricBufferProcessingOptions();
            testBufferProcessingOptions.BufferProcessingStrategy = MetricBufferProcessingStrategyImplementation.SizeLimitedLoopingWorkerThreadHybridBufferProcessor;
            testBufferProcessingOptions.BufferSizeLimit = 200;
            testBufferProcessingOptions.DequeueOperationLoopInterval = 20000;
            testBufferProcessingOptions.BufferProcessingFailureAction = MetricBufferProcessingFailureAction.DisableMetricLogging;
            Action<Exception> testBufferProcessingExceptionAction = (Exception bufferProcessingExceptionAction) => { Console.WriteLine(bufferProcessingExceptionAction.Message); };

            using (WorkerThreadBufferProcessorBase testBufferProcessor = testMetricsBufferProcessorFactory.GetBufferProcessor(testBufferProcessingOptions, testBufferProcessingExceptionAction, false))
            {

                Assert.IsAssignableFrom<SizeLimitedLoopingWorkerThreadHybridBufferProcessor>(testBufferProcessor);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "bufferSizeLimit" }, 200, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Int32>(new List<String>() { "dequeueOperationLoopInterval" }, 20000, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Action<Exception>>(new List<String>() { "bufferProcessingExceptionAction" }, testBufferProcessingExceptionAction, testBufferProcessor);
                NonPublicFieldAssert.HasValue<Boolean>(new List<String>() { "rethrowBufferProcessingException" }, false, testBufferProcessor);
            }
        }
    }
}
