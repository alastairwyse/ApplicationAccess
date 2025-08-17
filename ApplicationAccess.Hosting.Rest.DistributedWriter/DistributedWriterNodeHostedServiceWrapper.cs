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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationAccess.Distribution;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Writer;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Hosting.Rest.DistributedWriter
{
    /// <summary>
    /// Wraps an instance of <see cref="DistributedWriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DistributedWriterNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class DistributedWriterNodeHostedServiceWrapper
        : WriterNodeHostedServiceWrapperBase
        <
            DistributedWriterNode<String, String, String, String>, 
            DistributedAccessManager<String, String, String, String>,
            ManuallyFlushableBufferFlushStrategy
        >
    {
        #pragma warning disable 1591

        protected ManuallyFlushableBufferFlushStrategyHolder manuallyFlushableBufferFlushStrategyHolder;

        #pragma warning restore 1591

        /// <inheritdoc/>
        protected override String MetricLoggerCategoryName
        {
            get { return "ApplicationAccessDistributedWriterNode"; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedWriter.DistributedWriterNodeHostedServiceWrapper class.
        /// </summary>
        public DistributedWriterNodeHostedServiceWrapper
        (
            IOptions<DatabaseConnectionOptions> accessManagerDatabaseConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<EventPersistenceOptions> eventPersistenceOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            ManuallyFlushableBufferFlushStrategyHolder manuallyFlushableBufferFlushStrategyHolder, 
            TripSwitchActuator tripSwitchActuator,
            ILoggerFactory loggerFactory,
            ILogger<DistributedWriterNodeHostedServiceWrapper> logger
        )
            : base
        (
            accessManagerDatabaseConnectionOptions,
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
            this.manuallyFlushableBufferFlushStrategyHolder = manuallyFlushableBufferFlushStrategyHolder;
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override DistributedWriterNode<String, String, String, String> InitializeWriterNode()
        {
            var hashCodeGenerator = new DefaultStringHashCodeGenerator();

            return new DistributedWriterNode<String, String, String, String>(hashCodeGenerator, hashCodeGenerator, hashCodeGenerator, eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient);
        }

        /// <inheritdoc/>
        protected override DistributedWriterNode<String, String, String, String> InitializeWriterNodeWithMetricLogging()
        {
            var hashCodeGenerator = new DefaultStringHashCodeGenerator();

            return new DistributedWriterNode<String, String, String, String>(hashCodeGenerator, hashCodeGenerator, hashCodeGenerator, eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient, metricLogger);
        }

        /// <inheritdoc/>
        protected override ManuallyFlushableBufferFlushStrategy InitializeBufferFlushStrategy()
        {
            return new ManuallyFlushableBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit.Value,
                eventBufferFlushingOptions.FlushLoopInterval.Value,
                // If the event persister backup file is set, we want to set 'flushRemainingEventsAfterException' to true
                !(eventPersistenceOptions.EventPersisterBackupFilePath == null),
                eventBufferFlushingExceptionAction
            );
        }

        /// <inheritdoc/>
        protected override ManuallyFlushableBufferFlushStrategy InitializeBufferFlushStrategyWithMetricLogging()
        {
            return new ManuallyFlushableBufferFlushStrategy
            (
                eventBufferFlushingOptions.BufferSizeLimit.Value,
                eventBufferFlushingOptions.FlushLoopInterval.Value,
                // If the event persister backup file is set, we want to set 'flushRemainingEventsAfterException' to true
                !(eventPersistenceOptions.EventPersisterBackupFilePath == null),
                eventBufferFlushingExceptionAction,
                metricLogger
            );
        }

        /// <inheritdoc/>
        protected override void SetupHolderClasses()
        {
            base.SetupHolderClasses();
            manuallyFlushableBufferFlushStrategyHolder.ManuallyFlushableBufferFlushStrategy = eventBufferFlushStrategy;
        }

        #endregion
    }
}
