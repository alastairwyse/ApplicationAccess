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
    /// Unit tests for the ApplicationAccess.Hosting.DependencyFreeReaderWriterNode class.
    /// </summary>
    public class DependencyFreeReaderWriterNodeTests
    {
        private IAccessManagerEventBufferFlushStrategy mockEventBufferFlushStrategy;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        private IMetricLogger mockMetricLogger;
        private DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testDependencyFreeReaderWriterNode;

        [SetUp]
        protected void SetUp()
        {
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);
        }

        [TearDown]
        public void TearDown()
        {
            testDependencyFreeReaderWriterNode.Dispose();
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testDependencyFreeReaderWriterNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDependencyFreeReaderWriterNode, true);


            // Checks the metric logger set on the ConcurrentAccessManagerMetricLogger instance
            fieldNamePath = new List<String>() { "concurrentAccessManager", "metricLoggingWrapper", "metricLogger" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDependencyFreeReaderWriterNode, true);


            // Checks the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            // Checks the actual metric logger wrapped by the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger", "downstreamMetricLogger" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDependencyFreeReaderWriterNode, true);


            // Checks the metric logger is set on the event buffer
            fieldNamePath = new List<String>() { "eventBuffer", "metricLogger" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testDependencyFreeReaderWriterNode, true);
        }

        [Test]
        public void Constructor_EventBufferSetCorrectlyOnComposedFields()
        {
            DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testDependencyFreeReaderWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDependencyFreeReaderWriterNode);
        }

        [Test]
        public void Constructor_ConcurrentAccessManagerEventProcessorSetCorrectlyOnComposedFields()
        {
            DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testDependencyFreeReaderWriterNode;
            var fieldNamePath = new List<String>() { "concurrentAccessManager", "eventProcessor" };
            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDependencyFreeReaderWriterNode);


            testDependencyFreeReaderWriterNode = new DependencyFreeReaderWriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testDependencyFreeReaderWriterNode);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<ReaderWriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testDependencyFreeReaderWriterNode.Load(false);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderWriterNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderWriterNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndCallToPersistentReaderFails()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new Exception("Failed to load.");
            mockMetricLogger.Begin(Arg.Any<ReaderWriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testDependencyFreeReaderWriterNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderWriterNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderWriterNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetTrueAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<ReaderWriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testDependencyFreeReaderWriterNode.Load(true);
            });

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderWriterNodeLoadTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId, Arg.Any<ReaderWriterNodeLoadTime>());
            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Load_ThrowExceptionIfStorageIsEmptySetFalseAndPersistentStorageEmpty()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var mockException = new PersistentStorageEmptyException("Persistent storage is empty.");
            mockMetricLogger.Begin(Arg.Any<ReaderWriterNodeLoadTime>()).Returns(testBeginId);
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            testDependencyFreeReaderWriterNode.Load(false);

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderWriterNodeLoadTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ReaderWriterNodeLoadTime>());
        }
    }
}
