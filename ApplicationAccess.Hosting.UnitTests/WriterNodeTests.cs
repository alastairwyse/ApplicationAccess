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
    /// Unit tests for the ApplicationAccess.Hosting.WriterNode class.
    /// </summary>
    public class WriterNodeTests
    {
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
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
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEntityTypeHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventBulkCache = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );
        }

        [TearDown]
        public void TearDown()
        {
            testWriterNode.Dispose();
        }

        [Test]
        public void Constructor_MetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister,
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            // Checks the metric logger set on the ConcurrentAccessManagerMetricLogger instance
            fieldNamePath = new List<String>() { "concurrentAccessManager", "metricLoggingWrapper", "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            // Checks the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister,
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testWriterNode);


            // Checks the actual metric logger wrapped by the MappingMetricLogger within the MetricLoggingConcurrentDirectedGraph
            fieldNamePath = new List<String>() { "concurrentAccessManager", "userToGroupMap", "metricLogger", "downstreamMetricLogger" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<NullMetricLogger>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IMetricLogger>(fieldNamePath, mockMetricLogger, testWriterNode, true);
        }

        [Test]
        public void Constructor_EventBufferSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader,
                mockEventPersister, 
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterBuffer<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);
        }

        [Test]
        public void Constructor_EventBufferEventPersisterParameterSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var fieldNamePath = new List<String>() { "eventBuffer", "eventPersister" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventPersister, 
                mockEventCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy, 
                mockPersistentReader, 
                mockEventBulkPersister, 
                mockEventBulkCache, 
                mockMetricLogger
            );

            NonPublicFieldAssert.IsOfType<AccessManagerTemporalEventBulkPersisterDistributor<String, String, ApplicationScreen, AccessLevel>>(fieldNamePath, testWriterNode);
        }

        [Test]
        public void Constructor_HashCodeGeneratorsSetCorrectlyOnComposedFields()
        {
            WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;
            var userHashCodeGeneratorDistributorFieldNamePath = new List<String>() { "eventBuffer", "eventPersister", "userHashCodeGenerator" };
            var groupHashCodeGeneratorrDistributorFieldNamePath = new List<String>() { "eventBuffer", "eventPersister", "groupHashCodeGenerator" };
            var entityTypeHashCodeGeneratorrDistributorFieldNamePath = new List<String>() { "eventBuffer", "eventPersister", "entityTypeHashCodeGenerator" };
            var userHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "userHashCodeGenerator" };
            var groupHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "groupHashCodeGenerator" };
            var entityTypeHashCodeGeneratorFieldNamePath = new List<String>() { "eventBuffer", "entityTypeHashCodeGenerator" };
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventPersister,
                mockEventCache
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorDistributorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorrDistributorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorrDistributorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventBulkPersister,
                mockEventBulkCache
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventPersister,
                mockEventCache,
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorDistributorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorrDistributorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorrDistributorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);


            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>
            (
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventBufferFlushStrategy,
                mockPersistentReader,
                mockEventBulkPersister,
                mockEventBulkCache,
                mockMetricLogger
            );

            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(userHashCodeGeneratorFieldNamePath, mockUserHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(groupHashCodeGeneratorFieldNamePath, mockGroupHashCodeGenerator, testWriterNode, true);
            NonPublicFieldAssert.HasValue<IHashCodeGenerator<String>>(entityTypeHashCodeGeneratorFieldNamePath, mockEntityTypeHashCodeGenerator, testWriterNode, true);
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
