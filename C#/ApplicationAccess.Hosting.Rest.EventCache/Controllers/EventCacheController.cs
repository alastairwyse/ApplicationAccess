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

using Microsoft.AspNetCore.Mvc;
using ApplicationAccess.Persistence;
using Microsoft.Extensions.Logging;
using System;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Net.Mime;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Rest.EventCache.Controllers
{
    /// <summary>
    /// Controller which exposes an <see cref="AccessManagerTemporalEventBulkCache{TUser, TGroup, TComponent, TAccess}"/> object's methods as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public class EventCacheController : ControllerBase
    {
        protected IAccessManagerTemporalEventBulkPersister<String, String, String, String> eventPersister;
        protected IAccessManagerTemporalEventQueryProcessor<String, String, String, String> eventQueryProcessor;
        protected ILogger<EventCacheController> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.EventCache.Controllers.EventCacheController class.
        /// </summary>
        public EventCacheController(TemporalEventBulkPersisterHolder temporalEventBulkPersisterHolder, TemporalEventQueryProcessorHolder temporalEventQueryProcessorHolder, ILogger<EventCacheController> logger)
        {
            eventPersister = temporalEventBulkPersisterHolder.TemporalEventBulkPersister;
            eventQueryProcessor = temporalEventQueryProcessorHolder.TemporalEventQueryProcessor;
            this.logger = logger;
        }

        /// <summary>
        /// Adds the specified events to the cache.
        /// </summary>
        /// <param name="events">The events to cache.</param>
        /// <response code="201">The events were cached.</response>
        [HttpPost]
        [Route("eventBufferItems")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult CacheEvents([FromBody] List<TemporalEventBufferItemBase> events)
        {
            eventPersister.PersistEvents(events);

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// Retrieves all events from the cache, which occurred since the event with the specified id.
        /// </summary>
        /// <param name="priorEventdId">The id of the event to retrieve all events since.</param>
        /// <returns>An ordered array of events which occurred since the specified event, and not including the specified event.  Returned in order from least recent to most recent.</returns>
        /// <response code="404">An event with the specified id was not found in the cache.</response>
        [HttpGet]
        [Route("eventBufferItems")]
        [Produces(MediaTypeNames.Application.Json)]
        public IEnumerable<TemporalEventBufferItemBase> GetAllEventsSince([FromQuery, BindRequired] Guid priorEventdId)
        {
            try
            {
                return eventQueryProcessor.GetAllEventsSince(priorEventdId);
            }
            catch(EventNotCachedException eventNotCachedException)
            {
                throw new NotFoundException(eventNotCachedException.Message, priorEventdId.ToString());
            }
        }
    }
}
