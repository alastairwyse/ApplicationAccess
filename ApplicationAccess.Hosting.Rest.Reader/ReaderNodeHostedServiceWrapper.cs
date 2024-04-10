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
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Metrics;

namespace ApplicationAccess.Hosting.Rest.Reader
{
    /// <summary>
    /// Wraps an instance of <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> and associated components and initializes them using methods defined on the <see cref="IHostedService"/> interface, to allow hosting in ASP.NET.
    /// </summary>
    /// <remarks>StartAsync() constructs a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> instance (and its constructor parameters) from configuration, and calls methods like Start() and Load() on them, whist StopAsync() calls Stop(), Dispose(), etc.</remarks>
    public class ReaderNodeHostedServiceWrapper 
        : ReaderNodeHostedServiceWrapperBase<ReaderNode<String, String, String, String>, MetricLoggingConcurrentAccessManager<String, String, String, String>>
    {
        /// <inheritdoc/>
        protected override String MetricLoggerCategoryName
        {
            get { return "ApplicationAccessReaderNode"; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Reader.ReaderNodeHostedServiceWrapper class.
        /// </summary>
        public ReaderNodeHostedServiceWrapper
        (
            IOptions<AccessManagerOptions> accessManagerOptions,
            IOptions<AccessManagerSqlDatabaseConnectionOptions> accessManagerSqlDatabaseConnectionOptions,
            IOptions<EventCacheConnectionOptions> eventCacheConnectionOptions,
            IOptions<EventCacheRefreshOptions> eventCacheRefreshOptions,
            IOptions<MetricLoggingOptions> metricLoggingOptions,
            EntityQueryProcessorHolder entityQueryProcessorHolder,
            GroupQueryProcessorHolder groupQueryProcessorHolder,
            GroupToGroupQueryProcessorHolder groupToGroupQueryProcessorHolder,
            UserQueryProcessorHolder userQueryProcessorHolder,
            ILoggerFactory loggerFactory,
            ILogger<ReaderNodeHostedServiceWrapper> logger
        )
            : base
        (
            accessManagerOptions,
            accessManagerSqlDatabaseConnectionOptions,
            eventCacheConnectionOptions,
            eventCacheRefreshOptions,
            metricLoggingOptions,
            entityQueryProcessorHolder,
            groupQueryProcessorHolder,
            groupToGroupQueryProcessorHolder,
            userQueryProcessorHolder,
            loggerFactory,
            logger
        )
        {
        }

        #region Private/Protected Methods

        /// <inheritdoc/>
        protected override ReaderNode<String, String, String, String> InitializeReaderNode()
        {
            return new ReaderNode<String, String, String, String>(refreshStrategy, eventCacheClient, persistentReader, accessManagerOptions.StoreBidirectionalMappings.Value);
        }

        /// <inheritdoc/>
        protected override ReaderNode<String, String, String, String> InitializeReaderNodeWithMetricLogging()
        {
            return new ReaderNode<String, String, String, String>(refreshStrategy, eventCacheClient, persistentReader, accessManagerOptions.StoreBidirectionalMappings.Value, metricLogger);
        }

        #endregion
    }
}