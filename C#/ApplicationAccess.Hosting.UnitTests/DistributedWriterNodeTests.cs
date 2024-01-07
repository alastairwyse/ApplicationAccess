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
using ApplicationAccess.Persistence;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.DistributedWriterNode class.
    /// </summary>
    public class DistributedWriterNodeTests
    {
        private IAccessManagerEventBufferFlushStrategy mockEventBufferFlushStrategy;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventBulkPersister;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventBulkCache;
        private IMetricLogger mockMetricLogger;
        private DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedWriterNode;

        [SetUp]
        protected void SetUp()
        {
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkCache = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, false, mockMetricLogger);
        }

        [TearDown]
        public void TearDown()
        {
            testDistributedWriterNode.Dispose();
        }

        [Test]
        public void Constructor_ConcurrentAccessManagerStoreBidirectionalMappingsParameterSetCorrectlyOnComposedFields()
        {
            DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedWriterNode;
            var fieldNamePath = new List<String>() { "concurrentAccessManager", "storeBidirectionalMappings" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDistributedDistributedWriterNode);
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedWriterNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDistributedDistributedWriterNode);


            // Checks the metric logger set on the ConcurrentAccessManagerMetricLogger instance
            fieldNamePath = new List<String>() { "concurrentAccessManager", "metricLoggingWrapper", "metricLogger" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDistributedDistributedWriterNode, true);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDistributedDistributedWriterNode, true);


            // Checks the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedDistributedWriterNode);


            // Checks the actual metric logger wrapped by the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger", "downstreamMetricLogger" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDistributedDistributedWriterNode, true);


            // Checks the metric logger is set on the event buffer
            fieldNamePath = new List<String>() { "eventBuffer", "metricLogger" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDistributedDistributedWriterNode, true);
        }

        [Test]
        public void Constructor_EventBufferSetCorrectlyOnComposedFields()
        {
            DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);
        }

        [Test]
        public void Constructor_EventBufferEventPersisterParameterSetCorrectlyOnComposedFields()
        {
            DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer", "eventPersister" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);
        }

        [Test]
        public void Constructor_ConcurrentAccessManagerEventProcessorSetCorrectlyOnComposedFields()
        {
            DistributedWriterNode<String, String, ApplicationScreen, AccessLevel> testDistributedDistributedWriterNode;
            var fieldNamePath = new List<String>() { "concurrentAccessManager", "eventProcessor" };
            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);


            testDistributedDistributedWriterNode = new DistributedWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDistributedDistributedWriterNode);
        }
    }
}
