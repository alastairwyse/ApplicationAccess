﻿/*
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
    /// Unit tests for the ApplicationAccess.Hosting.ReaderWriterNode class.
    /// </summary>
    public class ReaderWriterNodeTests
    {
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
        private IAccessManagerEventBufferFlushStrategy mockEventBufferFlushStrategy;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        private IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventBulkPersister;
        private IMetricLogger mockMetricLogger;
        private ReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testReaderWriterNode;

        [SetUp]
        protected void SetUp()
        {
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEntityTypeHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );
        }

        [TearDown]
        public void TearDown()
        {
            testReaderWriterNode.Dispose();
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            ReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testReaderWriterNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            // Checks the metric logger set on the ConcurrentAccessManagerMetricLogger instance
            fieldNamePath = new List<String>() { "concurrentAccessManager", "metricLoggingWrapper", "metricLogger" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            // Checks the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testReaderWriterNode);


            // Checks the actual metric logger wrapped by the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger", "downstreamMetricLogger" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister,
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            // Checks the metric logger is set on the event buffer
            fieldNamePath = new List<String>() { "eventBuffer", "metricLogger" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testReaderWriterNode, true);
        }

        [Test]
        public void Constructor_EventBufferSetCorrectlyOnComposedFields()
        {
            ReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testReaderWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testReaderWriterNode);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testReaderWriterNode);
        }

        [Test]
        public void Constructor_HashCodeGeneratorsSetCorrectlyOnComposedFields()
        {
            ReaderWriterNode<String, String, ApplicationScreen, AccessLevel> testReaderWriterNode;
            var userHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "userHashCodeGenerator" };
            var groupHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "groupHashCodeGenerator" };
            var entityTypeHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "entityTypeHashCodeGenerator" };
            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventPersister
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventBulkPersister
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventPersister,
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testReaderWriterNode, true);


            testReaderWriterNode = new ReaderWriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventBulkPersister,
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testReaderWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testReaderWriterNode, true);
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
                testReaderWriterNode.Load(false);
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
                testReaderWriterNode.Load(true);
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
                testReaderWriterNode.Load(true);
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

            testReaderWriterNode.Load(false);

            mockMetricLogger.Received(1).Begin(Arg.Any<ReaderWriterNodeLoadTime>());
            mockPersistentReader.Received(1).Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<ReaderWriterNodeLoadTime>());
        }
    }
}
