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
using ApplicationAccess.Persistence;
using ApplicationAccess.UnitTests;
using NUnit.Framework;
using NSubstitute;
using ApplicationMetrics;

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
        private IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IMetricLogger mockMetricLogger;
        private WriterNode<String, String, ApplicationScreen, AccessLevel> testWriterNode;

        [SetUp]
        protected void SetUp()
        {
            mockEventBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testWriterNode = new WriterNode<String, String, ApplicationScreen, AccessLevel>(mockEventBufferFlushStrategy, mockPersistentReader, mockEventPersister, mockEventCache, mockMetricLogger);
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
