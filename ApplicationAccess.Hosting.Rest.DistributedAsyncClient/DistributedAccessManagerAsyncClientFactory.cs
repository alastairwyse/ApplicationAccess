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
using System.Collections.Generic;
using System.Net.Http;
using ApplicationAccess.Distribution;
using ApplicationMetrics;
using ApplicationLogging;
using Polly;

namespace ApplicationAccess.Hosting.Rest.DistributedAsyncClient
{
    /// <summary>
    /// Implmentation of <see cref="IDistributedAccessManagerAsyncClientFactory{TClientConfiguration, TUser, TGroup, TComponent, TAccess}"/> which creates distributed AccessManager clients from <see cref="AccessManagerRestClientConfiguration"/> objects.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager the client connects to.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager the client connects to.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager the client connects to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DistributedAccessManagerAsyncClientFactory<TUser, TGroup, TComponent, TAccess> : IDistributedAccessManagerAsyncClientFactory<AccessManagerRestClientConfiguration, TUser, TGroup, TComponent, TAccess>
    {
        /// <summary></summary>
        protected HttpClient httpClient;
        /// <summary>A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>The number of times an operation should be retried in the case of a transient error (e.g. network error).</summary>
        protected Int32 retryCount;
        /// <summary>The time in seconds between retries.</summary>
        protected Int32 retryInterval;
        /// <summary>Exception handling policy for HttpClient calls.</summary>
        protected AsyncPolicy exceptionHandingPolicy;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClientFactory class.
        /// </summary>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerAsyncClientFactory
        (
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
        {
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be less than 0.");
            if (retryCount > 59)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be greater than 59.");
            if (retryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be less than 0.");
            if (retryInterval > 120)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be greater than 120.");

            this.httpClient = httpClient;
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            exceptionHandingPolicy = null;
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.DistributedAccessManagerAsyncClientFactory class.
        /// </summary>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="exceptionHandingPolicy">Exception handling policy for HttpClient calls.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerAsyncClientFactory
        (
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AsyncPolicy exceptionHandingPolicy,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this
        (
            httpClient, 
            userStringifier,
            groupStringifier,
            applicationComponentStringifier,
            accessLevelStringifier,
            0,
            0,
            logger,
            metricLogger
        )
        {
            this.exceptionHandingPolicy = exceptionHandingPolicy;
        }

        /// <inheritdoc/>
        public IDistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess> GetClient(AccessManagerRestClientConfiguration configuration)
        {
            if (exceptionHandingPolicy != null)
            {
                return new DistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess>
                (
                    configuration.BaseUrl,
                    httpClient, 
                    userStringifier, 
                    groupStringifier,
                    applicationComponentStringifier,
                    accessLevelStringifier,
                    exceptionHandingPolicy, 
                    logger, 
                    metricLogger
                );
            }
            else
            {
                return new DistributedAccessManagerAsyncClient<TUser, TGroup, TComponent, TAccess>
                (
                    configuration.BaseUrl,
                    httpClient,
                    userStringifier,
                    groupStringifier,
                    applicationComponentStringifier,
                    accessLevelStringifier,
                    retryCount,
                    retryInterval,
                    logger,
                    metricLogger
                );
            }
        }
    }
}
