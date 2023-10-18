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
    /// Unit tests for the ApplicationAccess.Hosting.WriterNode class.
    /// </summary>
    public class WriterNodeTests
    {
        private IAccessManagerEventBufferFlushStrategy mockEventBufferFlushStrategy;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventBulkPersister;
        private IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventBulkCache;
        private IMetricLogger mockMetricLogger;
        private WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;

        [SetUp]
        protected void SetUp()
        {
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkCache = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, false, mockMetricLogger);
        }

        [Test]
        public void Constructor_ConcurrentAccessManagerStoreBidirectionalMappingsParameterSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "concurrentAccessManager", "storeBidirectionalMappings" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, false);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testWriterNode);
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            // Checks the metric logger set on the ConcurrentAccessManagerMetricLogger instance
            fieldNamePath = new List<String>() { "concurrentAccessManager", "metricLoggingWrapper", "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            // Checks the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            // Checks the actual metric logger wrapped by the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger", "downstreamMetricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);
        }

        [Test]
        public void Constructor_EventBufferSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);
        }

        [Test]
        public void Constructor_EventBufferEventPersisterParameterSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer", "eventPersister" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventBulkPersister, mockEventBulkCache, true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<WriterNodeLoadTime>()).Returns(testBeginId);
            mockMetricLogger.Begin(Arg.Any<WriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testWriterNode.Load(false);
            });

            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<WriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testWriterNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<WriterNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<WriterNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<WriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testWriterNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<WriterNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<WriterNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<WriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            testWriterNode.Load(false);

            mockMetricLogger.Received(1).Begin(Arg.Any<WriterNodeLoadTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<WriterNodeLoadTime>());
        }
    }
}
