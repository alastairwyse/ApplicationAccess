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
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Grpc;
using ApplicationAccess.Hosting.Rest.EventCache;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Hosting.Grpc.EventCache
{
    /// <summary>
    /// Service which exposes an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> object's methods as a gRPC service interface.
    /// </summary>
    public class EventCacheService : EventCacheRpc.EventCacheRpcBase
    {
        protected IAccessManagerTemporalEventBulkPersister<String, String, String, String> eventPersister;
        protected IAccessManagerTemporalEventQueryProcessor<String, String, String, String> eventQueryProcessor;
        protected ILogger<EventCacheService> logger;

        /// <summary>Used to convert <see cref="TemporalEventBufferItemBase"/> instances to and from their gRPC message equivalent.</summary>
        protected EventBufferItemToGrpcMessageConverter eventConverter;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.EventCache.Controllers.EventCacheController class.
        /// </summary>
        public EventCacheService(TemporalEventBulkPersisterHolder temporalEventBulkPersisterHolder, TemporalEventQueryProcessorHolder temporalEventQueryProcessorHolder, ILogger<EventCacheService> logger)
        {
            eventPersister = temporalEventBulkPersisterHolder.TemporalEventBulkPersister;
            eventQueryProcessor = temporalEventQueryProcessorHolder.TemporalEventQueryProcessor;
            this.logger = logger;
            eventConverter = new EventBufferItemToGrpcMessageConverter();
        }

        /// <summary>
        /// Adds the specified events to the cache.
        /// </summary>
        public override Task<Empty> CacheEvents(CacheEventsRequest request, ServerCallContext context)
        {
            List<TemporalEventBufferItemBase> eventsList = new(eventConverter.Convert(request.Events.Events));
            eventPersister.PersistEvents(eventsList);

            return Task.FromResult<Empty>(new Empty());
        }

        /// <summary>
        /// Retrieves all events from the cache, which occurred since the event with the specified id.
        /// </summary>
        public override Task<GetAllEventsSinceReply> GetAllEventsSince(GetAllEventsSinceRequest request, ServerCallContext context)
        {
            Guid priorEventdId = new Guid(request.PriorEventId.ToByteArray());
            IList<TemporalEventBufferItemBase> results;
            try
            {
                results = eventQueryProcessor.GetAllEventsSince(priorEventdId);
            }
            catch (EventNotCachedException eventNotCachedException)
            {
                throw new NotFoundException(eventNotCachedException.Message, priorEventdId.ToString());
            }
            GetAllEventsSinceReply reply = new();
            reply.Events = new Models.TemporalEventBufferItemList();
            reply.Events.Events.Add(eventConverter.Convert(results));

            return Task.FromResult<GetAllEventsSinceReply>(reply);
        }
    }
}
