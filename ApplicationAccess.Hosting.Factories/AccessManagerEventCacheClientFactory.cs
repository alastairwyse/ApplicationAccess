/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Hosting.Grpc.Client;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationAccess.Persistence;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Factories
{
    /// <summary>
    /// Factory for instances of <see cref="IAccessManagerEventCache{TUser, TGroup, TComponent, TAccess}"/> based on event cache connection options.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerEventCacheClientFactory<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Factories.AccessManagerEventCacheClientFactory class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        public AccessManagerEventCacheClientFactory
        (
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger
        )
        {
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.logger = logger;
            this.metricLogger = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Factories.AccessManagerEventCacheClientFactory class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerEventCacheClientFactory
        (
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Returns an <see cref="IAccessManagerEventCache{TUser, TGroup, TComponent, TAccess}"/> client instance which connects to an event cache.
        /// </summary>
        /// <param name="eventCacheConnectionOptions">The event cache connection options to use to create the client.</param>
        /// <returns>The <see cref="IAccessManagerEventCache{TUser, TGroup, TComponent, TAccess}"/> instance.</returns>
        public IAccessManagerEventCache<TUser, TGroup, TComponent, TAccess> GetClient(EventCacheConnectionOptions eventCacheConnectionOptions)
        {
            // Validity of the 'Host' property should have already been checked by the EventCacheConnectionOptionsValidator class.
            Uri hostAsUri = new Uri(eventCacheConnectionOptions.Host);

            if (eventCacheConnectionOptions.Protocol == Models.Protocol.Rest)
            {
                if (metricLogger == null)
                {
                    return new Rest.Client.EventCacheClient<TUser, TGroup, TComponent, TAccess>
                    (
                        hostAsUri, 
                        userStringifier, 
                        groupStringifier, 
                        applicationComponentStringifier, 
                        accessLevelStringifier, 
                        eventCacheConnectionOptions.RetryCount.Value, 
                        eventCacheConnectionOptions.RetryInterval.Value, 
                        logger, 
                        new NullMetricLogger()
                    );
                }
                else
                {
                    return new Rest.Client.EventCacheClient<TUser, TGroup, TComponent, TAccess>
                    (
                        hostAsUri,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier,
                        eventCacheConnectionOptions.RetryCount.Value,
                        eventCacheConnectionOptions.RetryInterval.Value, 
                        logger, 
                        metricLogger
                    );
                }
            }
            else if (eventCacheConnectionOptions.Protocol == Models.Protocol.Grpc)
            {
                if (metricLogger == null)
                {
                    return new Grpc.Client.EventCacheClient<TUser, TGroup, TComponent, TAccess>
                    (
                        hostAsUri,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier,
                        logger,
                        new NullMetricLogger()
                    );
                }
                else
                {
                    return new Grpc.Client.EventCacheClient<TUser, TGroup, TComponent, TAccess>
                    (
                        hostAsUri,
                        userStringifier,
                        groupStringifier,
                        applicationComponentStringifier,
                        accessLevelStringifier, 
                        logger,
                        metricLogger
                    );
                }
            }
            else
            {
                throw new Exception($"Encountered unhandled event cache connection protocol '{eventCacheConnectionOptions.Protocol}'.");
            }
        }
    }
}
