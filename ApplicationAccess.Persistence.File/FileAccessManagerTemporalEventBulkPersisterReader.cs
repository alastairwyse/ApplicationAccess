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
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Hosting.Rest;
using ApplicationAccess.Metrics;
using ApplicationAccess.Persistence.Models;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence.File
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersisterReader{TUser, TGroup, TComponent, TAccess}"/> which persists and allows reading in bulk of access manager events to and from the file system.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class is designed to persist a limited number of events, and act as a backup persister in a situation where a primary persister (e.g. database persister) has failed.</remarks>
    public class FileAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The name of the file used to store the persisted events.</summary>
        protected String eventFileName;
        /// <summary>The folder used to store the persisted events.</summary>
        protected String eventFolder;
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary><see cref="JsonConverter{T}"/> implementation for AccessManager events.</summary>
        protected TemporalEventBufferItemBaseConverter<TUser, TGroup, TComponent, TAccess> eventConverter;
        /// <summary>Serializer options which include member <see cref="eventConverter"/></summary>
        protected JsonSerializerOptions serializerOptions;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.File.FileAccessManagerTemporalEventBulkPersisterReader class.
        /// </summary>
        /// <param name="eventFilePath">The full path to the file used to store the persisted events.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public FileAccessManagerTemporalEventBulkPersisterReader
        (
            String eventFilePath,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        )
        {
            (String eventFolder, String eventFileName) = ValidateEventFilePath(eventFilePath, nameof(eventFilePath));
            this.eventFileName = eventFileName;
            this.eventFolder = eventFolder;
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            eventConverter = new TemporalEventBufferItemBaseConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
            serializerOptions = new JsonSerializerOptions() { Converters = { eventConverter } };
            this.logger = logger;
            this.metricLogger = new NullMetricLogger();
            TestFolderWriteAndRead(eventFolder);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.File.FileAccessManagerTemporalEventBulkPersisterReader class.
        /// </summary>
        /// <param name="eventFilePath">The full path to the file used to store the persisted events.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public FileAccessManagerTemporalEventBulkPersisterReader
        (
            String eventFilePath,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(eventFilePath, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            IList<TemporalEventBufferItemBase> existingEvents = GetAllEvents();
            String eventFilePath = Path.Combine(eventFolder, eventFileName);
            var newEvents = new List<TemporalEventBufferItemBase>(existingEvents);
            newEvents.AddRange(events);
            Guid beginId = metricLogger.Begin(new EventsFileWriteTime());
            try
            {
                using (var fileStream = new FileStream(eventFilePath, FileMode.CreateNew))
                {
                    JsonSerializer.Serialize<List<TemporalEventBufferItemBase>>(fileStream, newEvents, serializerOptions);
                }
            }
            catch (Exception e)
            {
                metricLogger.CancelBegin(beginId, new EventsFileWriteTime());
                throw new Exception($"Failed to write events to file '{eventFilePath}'.", e);
            }
            metricLogger.End(beginId, new EventsFileWriteTime());
            metricLogger.Add(new EventsWrittenToFile(), newEvents.Count);
            logger.Log(this, LogLevel.Warning, $"Wrote {newEvents.Count} events to file '{eventFilePath}'.");
        }

        /// <inheritdoc/>
        /// <remarks>Note that in this implementation, the file which holds the persisted events is deleted before returning.</remarks>
        public IList<TemporalEventBufferItemBase> GetAllEvents()
        {
            String eventFilePath = Path.Combine(eventFolder, eventFileName);
            IList<TemporalEventBufferItemBase> returnEvents = ReadAllEventsFromFile(eventFilePath);
            DeleteEventFile(eventFilePath);
            if (returnEvents.Count > 0)
            {
                metricLogger.Add(new EventsReadFromFile(), returnEvents.Count);
                logger.Log(this, LogLevel.Information, $"Read {returnEvents.Count} events from file '{eventFilePath}'.");
            }

            return returnEvents;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Validates the constructor 'eventFilePath' parameter, and returns the parameter split into folder and filename.
        /// </summary>
        /// <param name="eventFilePath">The value of the 'eventFilePath' constructor parameter.</param>
        /// <param name="eventFilePathParameterName">The name of the parameter holding the 'eventFilePath'.</param>
        /// <returns>The parameter 'eventFilePath' split into folder and filename.</returns>
        protected (String, String) ValidateEventFilePath(String eventFilePath, String eventFilePathParameterName)
        {
            if (String.IsNullOrWhiteSpace(eventFilePath) == true)
                throw new ArgumentException($"Parameter '{eventFilePathParameterName}' must contain a value.", eventFilePathParameterName);

            String fileName = Path.GetFileName(eventFilePath);
            if (fileName == "")
                throw new ArgumentException($"Parameter '{eventFilePathParameterName}' with value '{eventFilePath}' did not contain a file name.", eventFilePathParameterName);
            String folder = Path.GetDirectoryName(eventFilePath);
            if (folder == "")
                throw new ArgumentException($"Parameter '{eventFilePathParameterName}' with value '{eventFilePath}' did not contain a folder.", eventFilePathParameterName);

            return (folder, fileName);
        }

        /// <summary>
        /// Tests whether the folder at the specified path can have a file written to it, and read from it.
        /// </summary>
        /// <param name="folderPath">The path to the folder to test.</param>
        protected void TestFolderWriteAndRead(String folderPath)
        {
            String fileName = $"{Guid.NewGuid().ToString()}.txt";
            String fileContents = Guid.NewGuid().ToString();
            String filePath = Path.Combine(folderPath, fileName);

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.CreateNew))
                using (var streamWriter = new System.IO.StreamWriter(fileStream))
                {
                    streamWriter.Write(fileContents);
                    streamWriter.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to write to test file '{filePath}'.", e);
            }

            String testFileContents = null;
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open))
                using (var streamReader = new System.IO.StreamReader(fileStream))
                {
                    testFileContents = streamReader.ReadToEnd();
                    streamReader.Close();
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to read from test file '{filePath}'.", e);
            }
            if (testFileContents != fileContents)
            {
                throw new Exception($"Failed to correctly read contents of test file.  Expected to read contents '{fileContents}', but actually read '{testFileContents}'.");
            }
            try
            {
                System.IO.File.Delete(filePath);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to delete test file '{filePath}'.", e);
            }
        }

        /// <summary>
        /// Reads and returns all events from the persisted event file if it exists.
        /// </summary>
        /// <param name="eventFilePath">The full path to the file used to store the persisted events.</param>
        /// <returns>All events from the persisted event file, or an empty list if the event file doesn't exist/</returns>
        protected IList<TemporalEventBufferItemBase> ReadAllEventsFromFile(String eventFilePath)
        {
            if (System.IO.File.Exists(eventFilePath) == false)
            {
                return new List<TemporalEventBufferItemBase>();
            }
            else
            {
                try
                {
                    using (var fileStream = new FileStream(eventFilePath, FileMode.Open))
                    {
                        return JsonSerializer.Deserialize<List<TemporalEventBufferItemBase>>(fileStream, serializerOptions);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to read from persisted event file '{eventFilePath}'.", e);
                }
            }
        }

        /// <summary>
        /// Deletes the persisted event file if it exists.
        /// </summary>
        /// <param name="eventFilePath">The full path to the file used to store the persisted events.</param>
        protected void DeleteEventFile(String eventFilePath)
        {
            if (System.IO.File.Exists(eventFilePath) == true)
            {
                try
                {
                    System.IO.File.Delete(eventFilePath);
                }
                catch (Exception e)
                {
                    throw new Exception($"Failed to delete event file '{eventFilePath}'.", e);
                }
            }
        }

        #endregion
    }
}