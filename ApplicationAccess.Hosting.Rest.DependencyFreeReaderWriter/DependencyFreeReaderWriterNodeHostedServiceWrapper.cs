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
using Microsoft.AspNetCore.Mvc.Rendering;
using ApplicationAccess.Metrics;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.ReaderWriter;

namespace ApplicationAccess.Hosting.Rest.DependencyFreeReaderWriter
{
    /// <summary>
    /// Wraps an instance of <see cref="DependencyFreeReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="DependencyFreeReaderWriterNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class DependencyFreeReaderWriterNodeHostedServiceWrapper 
        : ReaderWriterNodeHostedServiceWrapperBase<DependencyFreeReaderWriterNode<String, String, String, String>, MetricLoggingDependencyFreeAccessManager<String, String, String, String>>
    {
        /// <inheritdoc/>
        protected override String MetricLoggerCategoryName
        {
            get { return "ApplicationAccessDependencyFreeReaderWriterNode"; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DependencyFreeReaderWriter.DependencyFreeReaderWriterNodeHostedServiceWrapper class.
        /// </summary>
        public DependencyFreeReaderWriterNodeHostedServiceWrapper
        (
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventBufferFlushingOptions> eventBufferFlushingOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityEventProcessorHolder entityEventProcessorHolder,
            EntityQueryProcessorHolder entityQueryProcessorHolder,
            GroupEventProcessorHolder groupEventProcessorHolder,
            GroupQueryProcessorHolder groupQueryProcessorHolder,
            GroupToGroupEventProcessorHolder groupToGroupEventProcessorHolder,
            GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder,
            UserEventProcessorHolder userEventProcessorHolder,
            UserQueryProcessorHolder userQueryProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<DependencyFreeReaderWriterNodeHostedServiceWrapper> logger
        )
            : base
        (
            accessManagerSqlDatabaseConnectionOptions, 
            eventBufferFlushingOptions,
            metricLoggingOptions,
            entityEventProcessorHolder,
            entityQueryProcessorHolder,
            groupEventProcessorHolder,
            groupQueryProcessorHolder,
            groupToGroupEventProcessorHolder,
            groupToGroupQueryProcessorHolder,
            userEventProcessorHolder,
            userQueryProcessorHolder,
            loggerFactory,
            logger
        )
        {
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override DependencyFreeReaderWriterNode<String, String, String, String> InitializeReaderWriterNode()
        {
            return new DependencyFreeReaderWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister);
        }

        /// <inheritdoc/>
        protected override DependencyFreeReaderWriterNode<String, String, String, String> InitializeReaderWriterNodeWithMetricLogging()
        {
            return new DependencyFreeReaderWriterNode<String, String, String, String>(eventBufferFlushStrategy, eventPersister, eventPersister, metricLogger);
        }

        #endregion
    }
}
