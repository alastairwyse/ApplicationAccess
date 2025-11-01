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
using System.Collections.Generic;
using System.Linq;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using ApplicationAccess.Hosting.Grpc.EventCache;
using ApplicationAccess.Hosting.Grpc.EventCache.V1;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Grpc.Client
{
    /// <summary>
    /// Client class which interfaces to an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a gRPC service.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class EventCacheClient<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>The gRPC channel to use to connect.</summary>
        protected GrpcChannel channel;
        /// <summary>Used to convert <see cref="TemporalEventBufferItemBase"/> instances to gRPC messages and vice versa.</summary>
        protected EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess> eventBufferItemToGrpcMessageConverter;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.EventCacheClient class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        public EventCacheClient
        (
            Uri url,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
        )
        {
            channel = GrpcChannel.ForAddress(url);
            eventBufferItemToGrpcMessageConverter = new EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.EventCacheClient class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="channelOptions">The options for configuring the gRPC channel.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        public EventCacheClient
        (
            Uri url,
            GrpcChannelOptions channelOptions, 
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier
        ) : this(url, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
        {
            channel.Dispose();
            channel = GrpcChannel.ForAddress(url, channelOptions);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.EventCacheClient class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public EventCacheClient
        (
            Uri url,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(url, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.EventCacheClient class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="channelOptions">The options for configuring the gRPC channel.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public EventCacheClient
        (
            Uri url,
            GrpcChannelOptions channelOptions,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        ) : this(url, channelOptions, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        #pragma warning disable 0436

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            var client = new EventCacheRpc.EventCacheRpcClient(channel);
            GetAllEventsSinceRequest request = new GetAllEventsSinceRequest() { PriorEventId = ByteString.CopyFrom(eventId.ToByteArray()) };
            GetAllEventsSinceReply reply;
            try
            {
                reply = client.GetAllEventsSince(request);
            }
            catch (RpcException rpcEx)
            {
                Console.WriteLine($"Server error: {rpcEx.Status.Detail}");
                var rpcStat = rpcEx.GetRpcStatus();
                
                var repcError = rpcStat.GetDetail<GrpcError>();
                Console.WriteLine("RPC Code: " + repcError.Code);
                Console.WriteLine("RPC Message: " + repcError.Message);

                throw;
            }
            catch (Exception e)
            {
                // TODO: Error handling here
                throw;
            }

            return eventBufferItemToGrpcMessageConverter.Convert(reply.Events.Events).ToList();
        }

        #pragma warning restore 0436

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            throw new NotImplementedException();
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the EventCacheClient.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~EventCacheClient()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    channel.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
