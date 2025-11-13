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
using Google.Rpc;
using Grpc.Core;
using Grpc.Net.Client;
using ApplicationAccess.Hosting.Grpc.EventCache;
using ApplicationAccess.Hosting.Grpc.EventCache.V1;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Hosting.Grpc.Client
{
    /// <summary>
    /// Client class which interfaces to an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> instance hosted as a gRPC service.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class EventCacheClient<TUser, TGroup, TComponent, TAccess> : AccessManagerClientBase, IAccessManagerTemporalEventQueryProcessor<TUser, TGroup, TComponent, TAccess>, IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Used to convert <see cref="TemporalEventBufferItemBase"/> instances to gRPC messages and vice versa.</summary>
        protected EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess> eventBufferItemToGrpcMessageConverter;

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
        ) : base(url)
        {
            eventBufferItemToGrpcMessageConverter = new EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
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
        ) : base (url, channelOptions)
        {
            eventBufferItemToGrpcMessageConverter = new EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
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
        ) : base(url, logger, metricLogger)
        { 
            eventBufferItemToGrpcMessageConverter = new EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
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
        ) : base(url, channelOptions, logger, metricLogger)
        {
            eventBufferItemToGrpcMessageConverter = new EventBufferItemToGrpcMessageConverter<TUser, TGroup, TComponent, TAccess>(userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
        }

        #pragma warning disable 0436

        /// <inheritdoc/>
        public IList<TemporalEventBufferItemBase> GetAllEventsSince(Guid eventId)
        {
            var client = new EventCacheRpc.EventCacheRpcClient(channel);
            GetAllEventsSinceRequest request = new() { PriorEventId = ByteString.CopyFrom(eventId.ToByteArray()) };
            GetAllEventsSinceReply reply;
            try
            {
                reply = client.GetAllEventsSince(request);
            }
            catch (Exception e)
            {
                RpcException rpcException = (RpcException)e;
                Google.Rpc.Status rpcStatus = rpcException.GetRpcStatus();
                if (rpcStatus != null)
                {
                    GrpcError grpcError = rpcStatus.GetDetail<GrpcError>();
                    if (grpcError != null)
                    {
                        if (rpcStatus.Code == (Int32)Code.Unavailable && grpcError.Code == nameof(EventCacheEmptyException))
                        {
                            throw new EventCacheEmptyException(grpcError.Message);
                        }
                        else if (rpcStatus.Code == (Int32)Code.NotFound && grpcError.Code == nameof(NotFoundException))
                        {
                            throw new EventNotCachedException(grpcError.Message);
                        }
                    }
                }
                HandleRpcException(nameof(client.GetAllEventsSince), e);
                throw;
            }

            return eventBufferItemToGrpcMessageConverter.Convert(reply.Events.Events).ToList();
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            var client = new EventCacheRpc.EventCacheRpcClient(channel);
            TemporalEventBufferItemList convertedEventList = new();
            foreach (TemporalEventBufferItemListItem currentConvertedEvent in eventBufferItemToGrpcMessageConverter.Convert(events))
            {
                convertedEventList.Events.Add(currentConvertedEvent);
            }
            CacheEventsRequest request = new() { Events = convertedEventList };
            client.CacheEvents(request);
        }

        #pragma warning restore 0436
    }
}
