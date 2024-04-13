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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Writer;
using ApplicationAccess.Metrics;

namespace ApplicationAccess.Hosting.Rest.DistributedWriter
{
    /// <summary>
    /// Wraps an instance of <see cref="DistributedWriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DistributedWriterNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class DistributedWriterNodeHostedServiceWrapper
        : WriterNodeHostedServiceWrapperBase<DistributedWriterNode<String, String, String, String>, DistributedAccessManager<String, String, String, String>>
    {
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
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<DistributedWriterNodeHostedServiceWrapper> logger
        )
            : base
        (
            accessManagerSqlDatabaseConnectionOptions,
            eventBufferFlushingOptions,
            eventCacheConnectionOptions,
            metricLoggingOptions,
            entityEventProcessorHolder,
            groupEventProcessorHolder,
            groupToGroupEventProcessorHolder,
            userEventProcessorHolder,
            loggerFactory,
            logger
        )
        {
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override DistributedWriterNode<String, String, String, String> InitializeWriterNode()
        {
            return new DistributedWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient);
        }

        /// <inheritdoc/>
        protected override DistributedWriterNode<String, String, String, String> InitializeWriterNodeWithMetricLogging()
        {
            return new DistributedWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, eventCacheClient, metricLogger);
        }

        #endregion
    }
}
