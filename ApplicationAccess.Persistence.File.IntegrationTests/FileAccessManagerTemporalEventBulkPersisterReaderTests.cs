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
using System.Globalization;
using System.IO;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence.File;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Persistence.File.IntegrationTests
{
    /// <summary>
    /// Integration tests for the ApplicationAccess.Persistence.File.FileAccessManagerTemporalEventBulkPersisterReader class.
    /// </summary>
    public class FileAccessManagerTemporalEventBulkPersisterReaderTests
    {
        protected const String testFolderName = "IntegrationTest-Temp";
        protected const String testEventFileName = "FileAccessManagerTemporalEventBulkPersisterReader-Events.json";
        protected String testEventFolder;
        protected IApplicationLogger mockLogger;
        protected IMetricLogger mockMetricLogger;
        protected FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String> testFileAccessManagerTemporalEventBulkPersisterReader;

        [OneTimeSetUp]
        protected void OneTimeSetUp()
        {
            testEventFolder = Path.Combine(Directory.GetCurrentDirectory(), testFolderName);
            CreateTestFolder();
        }

        [SetUp]
        protected void SetUp()
        {
            mockLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            DeleteTestEventFile();
        }

        [OneTimeTearDown]
        protected void OneTimeTearDown()
        {
            DeleteTestFolder();
        }

        [Test]
        public void Constructor_EventFilePathParameterEmpty()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
                (
                    " ",
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'eventFilePath' must contain a value."));
            Assert.AreEqual("eventFilePath", e.ParamName);
        }

        [Test]
        public void Constructor_EventFilePathParameterDoesntContainFileName()
        {
            String folderName = @"C:\Temp\";

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
                (
                    folderName,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'eventFilePath' with value '{folderName}' did not contain a file name."));
            Assert.AreEqual("eventFilePath", e.ParamName);
        }

        [Test]
        public void Constructor_EventFilePathParameterDoesntContainFolder()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
                (
                    testEventFileName,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    mockLogger,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'eventFilePath' with value '{testEventFileName}' did not contain a folder."));
            Assert.AreEqual("eventFilePath", e.ParamName);
        }

        [Test]
        public void Constructor_CannotWriteToPathInEventFilePathParameter()
        {
            String invalidFolder = $"C:\\{Guid.NewGuid().ToString()}\\";
            String invalidEventFilePath = Path.Combine(invalidFolder, testEventFileName);

            var e = Assert.Throws<Exception>(delegate
            {
                testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
                (
                    invalidEventFilePath,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    mockLogger,
                    mockMetricLogger
                );

            });

            Assert.That(e.Message, Does.StartWith($"Failed to write to test file '{invalidFolder}"));
        }

        [Test]
        public void GetAllEvents_ExceptionWhenReading()
        {
            String testEventFilePath = Path.Combine(testEventFolder, testEventFileName);
            using (var fileStream = new FileStream(testEventFilePath, FileMode.Create))
            using (var streamWriter = new System.IO.StreamWriter(fileStream))
            {
                streamWriter.Write("Not Events");
                streamWriter.Close();
            }
            testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
            (
                testEventFilePath,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                mockLogger,
                mockMetricLogger
            );

            var e = Assert.Throws<Exception>(delegate
            {
                testFileAccessManagerTemporalEventBulkPersisterReader.GetAllEvents();
            });

            Assert.That(e.Message, Does.StartWith($"Failed to read from persisted event file '{testEventFilePath}'."));
            Assert.IsAssignableFrom<System.Text.Json.JsonException>(e.InnerException);
        }

        [Test]
        public void PersistEventsGetAllEvents()
        {
            String testEventFilePath = Path.Combine(testEventFolder, testEventFileName);
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var testEvents = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "User1", CreateDataTimeFromString("2024-08-25 15:57:00")),
                new GroupEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "Group1", CreateDataTimeFromString("2024-08-25 15:57:10")),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.NewGuid(), EventAction.Add, "User1", "Group1", CreateDataTimeFromString("2024-08-25 15:57:20"))
            };
            testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
            (
                testEventFilePath,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                mockLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<EventsFileWriteTime>()).Returns(testBeginId);

            testFileAccessManagerTemporalEventBulkPersisterReader.PersistEvents(testEvents);
            IList<TemporalEventBufferItemBase> results = testFileAccessManagerTemporalEventBulkPersisterReader.GetAllEvents();

            mockMetricLogger.Received(1).Begin(Arg.Any<EventsFileWriteTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EventsFileWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToFile>(), 3);
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Warning, $"Wrote 3 events to file '{testEventFilePath}'.");
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromFile>(), 3);
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Information, $"Read 3 events from file '{testEventFilePath}'.");
            Assert.AreEqual(3, results.Count);
            Assert.IsAssignableFrom<UserEventBufferItem<String>>(results[0]);
            Assert.AreEqual(testEvents[0].EventId, results[0].EventId);
            Assert.AreEqual(testEvents[0].EventAction, results[0].EventAction);
            Assert.AreEqual(testEvents[0].OccurredTime, results[0].OccurredTime);
            Assert.AreEqual(((UserEventBufferItem<String>)testEvents[0]).User, ((UserEventBufferItem<String>)results[0]).User);
            Assert.IsAssignableFrom<GroupEventBufferItem<String>>(results[1]);
            Assert.AreEqual(testEvents[1].EventId, results[1].EventId);
            Assert.AreEqual(testEvents[1].EventAction, results[1].EventAction);
            Assert.AreEqual(testEvents[1].OccurredTime, results[1].OccurredTime);
            Assert.AreEqual(((GroupEventBufferItem<String>)testEvents[1]).Group, ((GroupEventBufferItem<String>)results[1]).Group);
            Assert.IsAssignableFrom<UserToGroupMappingEventBufferItem<String, String>>(results[2]);
            Assert.AreEqual(testEvents[2].EventId, results[2].EventId);
            Assert.AreEqual(testEvents[2].EventAction, results[2].EventAction);
            Assert.AreEqual(testEvents[2].OccurredTime, results[2].OccurredTime);
            Assert.AreEqual(((UserToGroupMappingEventBufferItem<String, String>)testEvents[2]).User, ((UserToGroupMappingEventBufferItem<String, String>)results[2]).User);
            Assert.AreEqual(((UserToGroupMappingEventBufferItem<String, String>)testEvents[2]).Group, ((UserToGroupMappingEventBufferItem<String, String>)results[2]).Group);
            Assert.IsFalse(System.IO.File.Exists(testEventFilePath));
        }

        [Test]
        public void PersistEventsGetAllEvents_MultipleCallsToPersistEvents()
        {
            String testEventFilePath = Path.Combine(testEventFolder, testEventFileName);
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var testEvents1 = new List<TemporalEventBufferItemBase>()
            {
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "User1", CreateDataTimeFromString("2024-08-25 15:57:00")),
                new GroupEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "Group1", CreateDataTimeFromString("2024-08-25 15:57:10")),
                new UserToGroupMappingEventBufferItem<String, String>(Guid.NewGuid(), EventAction.Add, "User1", "Group1", CreateDataTimeFromString("2024-08-25 15:57:20"))
            };
            var testEvents2 = new List<TemporalEventBufferItemBase>()
            {
                new EntityTypeEventBufferItem(Guid.NewGuid(), EventAction.Add, "Clients", CreateDataTimeFromString("2024-08-25 15:57:30")),
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Remove, "User1", CreateDataTimeFromString("2024-08-25 15:57:40")),
            };
            testFileAccessManagerTemporalEventBulkPersisterReader = new FileAccessManagerTemporalEventBulkPersisterReader<String, String, String, String>
            (
                testEventFilePath,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                mockLogger,
                mockMetricLogger
            );
            mockMetricLogger.Begin(Arg.Any<EventsFileWriteTime>()).Returns(testBeginId);

            testFileAccessManagerTemporalEventBulkPersisterReader.PersistEvents(testEvents1);
            testFileAccessManagerTemporalEventBulkPersisterReader.PersistEvents(testEvents2);
            IList<TemporalEventBufferItemBase> results = testFileAccessManagerTemporalEventBulkPersisterReader.GetAllEvents();

            mockMetricLogger.Received(2).Begin(Arg.Any<EventsFileWriteTime>());
            mockMetricLogger.Received(2).End(testBeginId, Arg.Any<EventsFileWriteTime>());
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToFile>(), 3);
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Warning, $"Wrote 3 events to file '{testEventFilePath}'.");
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromFile>(), 3);
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Information, $"Read 3 events from file '{testEventFilePath}'.");
            mockMetricLogger.Received(1).Add(Arg.Any<EventsWrittenToFile>(), 5); ;
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Warning, $"Wrote 5 events to file '{testEventFilePath}'.");
            mockMetricLogger.Received(1).Add(Arg.Any<EventsReadFromFile>(), 5);
            mockLogger.Received(1).Log(testFileAccessManagerTemporalEventBulkPersisterReader, LogLevel.Information, $"Read 5 events from file '{testEventFilePath}'.");
            Assert.AreEqual(5, results.Count);
            Assert.IsAssignableFrom<UserEventBufferItem<String>>(results[0]);
            Assert.AreEqual(testEvents1[0].EventId, results[0].EventId);
            Assert.AreEqual(testEvents1[0].EventAction, results[0].EventAction);
            Assert.AreEqual(testEvents1[0].OccurredTime, results[0].OccurredTime);
            Assert.AreEqual(((UserEventBufferItem<String>)testEvents1[0]).User, ((UserEventBufferItem<String>)results[0]).User);
            Assert.IsAssignableFrom<GroupEventBufferItem<String>>(results[1]);
            Assert.AreEqual(testEvents1[1].EventId, results[1].EventId);
            Assert.AreEqual(testEvents1[1].EventAction, results[1].EventAction);
            Assert.AreEqual(testEvents1[1].OccurredTime, results[1].OccurredTime);
            Assert.AreEqual(((GroupEventBufferItem<String>)testEvents1[1]).Group, ((GroupEventBufferItem<String>)results[1]).Group);
            Assert.IsAssignableFrom<UserToGroupMappingEventBufferItem<String, String>>(results[2]);
            Assert.AreEqual(testEvents1[2].EventId, results[2].EventId);
            Assert.AreEqual(testEvents1[2].EventAction, results[2].EventAction);
            Assert.AreEqual(testEvents1[2].OccurredTime, results[2].OccurredTime);
            Assert.AreEqual(((UserToGroupMappingEventBufferItem<String, String>)testEvents1[2]).User, ((UserToGroupMappingEventBufferItem<String, String>)results[2]).User);
            Assert.AreEqual(((UserToGroupMappingEventBufferItem<String, String>)testEvents1[2]).Group, ((UserToGroupMappingEventBufferItem<String, String>)results[2]).Group);
            Assert.IsAssignableFrom<EntityTypeEventBufferItem>(results[3]);
            Assert.AreEqual(testEvents2[0].EventId, results[3].EventId);
            Assert.AreEqual(testEvents2[0].EventAction, results[3].EventAction);
            Assert.AreEqual(testEvents2[0].OccurredTime, results[3].OccurredTime);
            Assert.AreEqual(((EntityTypeEventBufferItem)testEvents2[0]).EntityType, ((EntityTypeEventBufferItem)results[3]).EntityType);
            Assert.IsAssignableFrom<UserEventBufferItem<String>>(results[4]);
            Assert.AreEqual(testEvents2[1].EventId, results[4].EventId);
            Assert.AreEqual(testEvents2[1].EventAction, results[4].EventAction);
            Assert.AreEqual(testEvents2[1].OccurredTime, results[4].OccurredTime);
            Assert.AreEqual(((UserEventBufferItem<String>)testEvents2[1]).User, ((UserEventBufferItem<String>)results[4]).User);
            Assert.IsFalse(System.IO.File.Exists(testEventFilePath));
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates the test event folder if it doesn't exist.
        /// </summary>
        protected void CreateTestFolder()
        {
            if (Directory.Exists(testEventFolder) == false)
            {
                Directory.CreateDirectory(testEventFolder);
            }
        }

        /// <summary>
        /// Deletes the test event folder if it exists.
        /// </summary>
        protected void DeleteTestFolder()
        {
            if (Directory.Exists(testEventFolder) == true)
            {
                Directory.Delete(testEventFolder);
            }
        }

        /// <summary>
        /// Deletes the test event file if it exists.
        /// </summary>
        protected void DeleteTestEventFile()
        {
            String eventFilePath = Path.Combine(testEventFolder, testEventFileName);
            if (System.IO.File.Exists(eventFilePath) == true)
            {
                System.IO.File.Delete(eventFilePath);
            }
        }

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion
    }
}