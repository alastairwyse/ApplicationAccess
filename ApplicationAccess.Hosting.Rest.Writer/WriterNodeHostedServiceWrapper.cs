﻿/*
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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Hosting.Rest.Writer
{
    /// <summary>
    /// Wraps an instance of <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="WriterNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class WriterNodeHostedServiceWrapper
        : WriterNodeHostedServiceWrapperBase
        <
            WriterNode<String, String, String, String>, 
            MetricLoggingConcurrentAccessManager<String, String, String, String>, 
            SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
        >
    {
        /// <inheritdoc/>
        protected override String MetricLoggerCategoryName
        {
            get { return "ApplicationAccessWriterNode"; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Writer.WriterNodeHostedServiceWrapper class.
        /// </summary>
        public WriterNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<EventPersistenceOptions> eventPersistenceOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<WriterNodeHostedServiceWrapper> logger
        )
            : base
        (
            accessManagerSqlDatabaseConnectionOptions,
            eventBufferFlushingOptions,
            eventPersistenceOptions, 
            eventCacheConnectionOptions,
            metricLoggingOptions,
            entityEventProcessorHolder,
            groupEventProcessorHolder,
            groupToGroupEventProcessorHolder,
            userEventProcessorHolder,
            tripSwitchActuator, 
            loggerFactory,
            logger
        )
        {
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override WriterNode<String, String, String, String> InitializeWriterNode()
        {
            var hashCodeGenerator = new DefaultStringHashCodeGenerator();

            return new WriterNode<String, String, String, String>(hashCodeGenerator, hashCodeGenerator, hashCodeGenerator, eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient);
        }

        /// <inheritdoc/>
        protected override WriterNode<String, String, String, String> InitializeWriterNodeWithMetricLogging()
        {
            var hashCodeGenerator = new DefaultStringHashCodeGenerator();

            return new WriterNode<String, String, String, String>(hashCodeGenerator, hashCodeGenerator, hashCodeGenerator, eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient, metricLogger);
        }

        /// <inheritdoc/>
        protected override SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy InitializeBufferFlushStrategy()
        {
            return new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit.Value,
                eventBufferFlushingOptions.FlushLoopInterval.Value,
                // If the event persister backup file is set, we want to set 'flushRemainingEventsAfterException' to true
                !(eventPersistenceOptions.EventPersisterBackupFilePath == null),
                eventBufferFlushingExceptionAction
            );
        }

        /// <inheritdoc/>
        protected override SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy InitializeBufferFlushStrategyWithMetricLogging()
        {
            return new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit.Value,
                eventBufferFlushingOptions.FlushLoopInterval.Value,
                // If the event persister backup file is set, we want to set 'flushRemainingEventsAfterException' to true
                !(eventPersistenceOptions.EventPersisterBackupFilePath == null),
                eventBufferFlushingExceptionAction, 
                metricLogger
            );
        }

        #endregion
    }
}
