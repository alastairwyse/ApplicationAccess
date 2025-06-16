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
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Redistribution.Metrics;
using ApplicationLogging;
using ApplicationMetrics;

namespace ApplicationAccess.Redistribution
{
    /// <summary>
    /// Implementation of <see cref="IEventPersisterBuffer"/> which filters out any duplicate events for primary elements before passing to another instance of <see cref="IEventPersisterBuffer"/>(i.e. following the <see href="https://en.wikipedia.org/wiki/Decorator_pattern">GOF decorator pattern).</see>
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager instance the events are filtered for.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager instance the events are filtered for.</typeparam>
    /// <remarks>
    /// <para>When elements from two shard groups are merged there may be duplicate primary elements defined in both.  This class provides a mechanism to filter out the duplicates before pushing the events to a new shard group.</para>
    /// <para>For very large source shard groups, need to consider the number of primary elements and potential memory usage, since this class stores all current/active primary elements to implement the filtering.</para>
    /// </remarks>
    public class PrimaryElementEventDuplicateFilter<TUser, TGroup> : IEventPersisterBuffer
    {
        // When merging events from two sources, merging of primary element 'add' events is straightforward... we keep track of any primary elements which are currently stored, and 
        //   ignore a second 'add' events for that element.
        // However 'remove' events are slightly harder to handle due to the way the delete events are generated in a distributed access manager instance. When a distributed operation
        //   coordinator coordinates a primary element 'remove' operation, it sends that operation to all relevant underlying shard groups in parallel, however they will actually
        //   reach and be processed at each shard group at slightly different times.  Focusing on just the two shard groups whose events are being merged, there is a possibility that
        //   in the time between receiving and processing the primary element 'remove' operation at each shard group, another event involving that primary element is received and 
        //   processed by the second shard group (i.e. the second one to process the primary element 'remove' operation)... illustrated below...
        //   
        //   Time   Action
        //   -------------
        //   T1     Remove user 'user1' received and processed by shard group A
        //   T2     Add user to group mapping involving 'user1' received and processed by shard group B
        //   T3     Remove user 'user1' received and processed by shard group B
        //
        // If we follow the same behaviour as for 'add' events (i.e. ignoring the second primary element 'remove' event), the sequence of actions above will break when we try to persist 
        //   to relational databases, since 'user1' will be deleted from the merged shared group at T1.  When the add user to group mapping event is received at T2, it will break as 
        //   'user1' doesn't exist in the merged shard group.
        // Hence for 'remove' events, we only pass the event to the underlying IEventPersisterBuffer instance if the element being removed only exists in one of the source shard groups.
        //   If it exists in both, we assume that another 'remove' event will subsequently be received (from the 'other' shard group) due to the way the distributed operation
        //   coordinator coordinates a primary element 'remove' operation described above.
        //
        // Additionally the class checks for anomalous sequences of events... e.g. receiving two sequential 'remove' events for the same element, and if constructor parameter
        //   'ignoreInvalidPrimaryElementEvents' is set true an exception will be thrown in these cases (if set false, the anomalous events in such sequences are ignored/filtered).

        /// <summary>The id of the first source event most recently persisted (null if no events have been persisted from the first source).</summary>
        protected Nullable<Guid> sourceShardGroup1LastPersistedEventId;
        /// <summary>The id of the second source event most recently persisted (null if no events have been persisted from the second source).</summary>
        protected Nullable<Guid> sourceShardGroup2LastPersistedEventId;
        /// <summary>Holds all current users defined in the events and which source shard group(s) the event(s) came from.</summary>
        protected Dictionary<TUser, PrimaryElementEventSources> currentUsers;
        /// <summary>Holds all current groups defined in the events and which source shard group(s) the event(s) came from.</summary>
        protected Dictionary<TGroup, PrimaryElementEventSources> currentGroups;
        /// <summary>Holds all current entity types defined in the events and which source shard group(s) the event(s) came from.</summary>
        protected Dictionary<String, PrimaryElementEventSources> currentEntityTypes;
        /// <summary>Holds all current entities defined in the events and which source shard group(s) the event(s) came from.</summary>
        protected Dictionary<Tuple<String, String>, PrimaryElementEventSources> currentEntities;
        /// <summary>The <see cref="IEventPersisterBuffer"/> to pass unfiltered events to.</summary>
        protected IEventPersisterBuffer eventPersisterBuffer;
        /// <summary>Whether invalid events for primary elements (e.g. receiving a delete event for an element from a source where that element doesn't already exist) should be ignored.  Setting to false will throw an exception when such events are received.</summary>
        protected Boolean ignoreInvalidPrimaryElementEvents;
        /// <summary>Count of the number of duplicate 'add' events received for primary elements.</summary>
        protected Int32 duplicatePrimaryAddEventsReceived;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// The <see cref="IEventPersisterBuffer"/> to pass unfiltered events to.
        /// </summary>
        public IEventPersisterBuffer EventPersisterBuffer
        {
            set { eventPersisterBuffer = value; }
        }

        /// <summary>
        /// Count of the number of duplicate 'add' events received for primary elements.
        /// </summary>
        public Int32 DuplicatePrimaryAddEventsReceived
        {
            get { return duplicatePrimaryAddEventsReceived; } 
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.PrimaryElementEventDuplicateFilter class.
        /// </summary>
        /// <param name="ignoreInvalidPrimaryElementEvents">Whether invalid events for primary elements (e.g. receiving a delete event for an element from a source where that element doesn't already exist) should be ignored.  Setting to false will throw an exception when such events are received.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public PrimaryElementEventDuplicateFilter(Boolean ignoreInvalidPrimaryElementEvents, IApplicationLogger logger, IMetricLogger metricLogger)
        {
            sourceShardGroup1LastPersistedEventId = null;
            sourceShardGroup2LastPersistedEventId = null;
            currentUsers = new Dictionary<TUser, PrimaryElementEventSources>();
            currentGroups = new Dictionary<TGroup, PrimaryElementEventSources>();
            currentEntityTypes = new Dictionary<String, PrimaryElementEventSources>();
            currentEntities = new Dictionary<Tuple<String, String>, PrimaryElementEventSources>();
            this.ignoreInvalidPrimaryElementEvents = ignoreInvalidPrimaryElementEvents;
            duplicatePrimaryAddEventsReceived = 0;
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public Tuple<Nullable<Guid>, Nullable<Guid>> BufferEvent(TemporalEventBufferItemBase inputEvent, Boolean sourcedFromFirstShardGroup)
        {
            if (eventPersisterBuffer == null)
                throw new InvalidOperationException($"Property '{nameof(EventPersisterBuffer)}' has not been set.");

            Boolean bufferEvent = true;
            if (inputEvent.GetType() == typeof(UserEventBufferItem<TUser>))
            {
                bufferEvent = FilterPrimaryElementEvent(inputEvent, sourcedFromFirstShardGroup, currentUsers, "user", ((UserEventBufferItem<TUser>)inputEvent).User);
            }
            else if (inputEvent.GetType() == typeof(GroupEventBufferItem<TGroup>))
            {
                bufferEvent = FilterPrimaryElementEvent(inputEvent, sourcedFromFirstShardGroup, currentGroups, "group", ((GroupEventBufferItem<TGroup>)inputEvent).Group);
            }
            else if (inputEvent.GetType() == typeof(EntityTypeEventBufferItem))
            {
                bufferEvent = FilterPrimaryElementEvent(inputEvent, sourcedFromFirstShardGroup, currentEntityTypes, "entity type", ((EntityTypeEventBufferItem)inputEvent).EntityType);
            }
            else if (inputEvent.GetType() == typeof(EntityEventBufferItem))
            {
                var typedInputEvent = (EntityEventBufferItem)inputEvent;
                String entityType = typedInputEvent.EntityType;
                String entity = typedInputEvent.Entity;
                bufferEvent = FilterPrimaryElementEvent(inputEvent, sourcedFromFirstShardGroup, currentEntities, "entity", Tuple.Create(entityType, entity));
            }

            if (bufferEvent == true)
            {
                Tuple<Nullable<Guid>, Nullable<Guid>> result = eventPersisterBuffer.BufferEvent(inputEvent, sourcedFromFirstShardGroup);
                sourceShardGroup1LastPersistedEventId = result.Item1;
                sourceShardGroup2LastPersistedEventId = result.Item2;
                return result;
            }
            else
            {
                return Tuple.Create(sourceShardGroup1LastPersistedEventId, sourceShardGroup2LastPersistedEventId);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Provides generic filtering for <see cref="TemporalEventBufferItemBase"/> instances regardless of the derived type.
        /// </summary>
        /// <typeparam name="TElement">The type of element contained within the event.</typeparam>
        /// <param name="inputEvent">The event to buffer.</param>
        /// <param name="sourcedFromFirstShardGroup">Whether the event was sourced from the first shard group being merged (assumed that the event was sourced from the second shard group if set false).</param>
        /// <param name="currentElementsDictionary">The dictionary which holds all current elements and which source shard group(s) the event(s) came from.</param>
        /// <param name="eventElementType">The type of element contained within the event (stringified version to use in logging and exception messages).</param>
        /// <param name="eventElementValue">The value of the element within the event in parameter <paramref name="inputEvent"/>"/>.</param>
        /// <returns>Whether the event should be passed to the subsequent <see cref="IEventPersisterBuffer"/> instance.</returns>
        protected Boolean FilterPrimaryElementEvent<TElement>(TemporalEventBufferItemBase inputEvent, Boolean sourcedFromFirstShardGroup, Dictionary<TElement, PrimaryElementEventSources> currentElementsDictionary, String eventElementType, TElement eventElementValue) 
        {
            if (inputEvent.EventAction == EventAction.Add)
            {
                if (currentElementsDictionary.ContainsKey(eventElementValue) == false)
                {
                    if (sourcedFromFirstShardGroup == true)
                    {
                        currentElementsDictionary.Add(eventElementValue, new PrimaryElementEventSources(true, false));
                    }
                    else
                    {
                        currentElementsDictionary.Add(eventElementValue, new PrimaryElementEventSources(false, true));
                    }

                    return true;
                }
                else
                {
                    if ((currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup1 == true && sourcedFromFirstShardGroup == true) || 
                        (currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup2 == true && sourcedFromFirstShardGroup == false))
                    {
                        if (ignoreInvalidPrimaryElementEvents == true)
                        {
                            logger.Log(this, LogLevel.Error, GenerateDuplicateAddEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                            metricLogger.Increment(new InvalidAddPrimaryElementEventReceived());
                        }
                        else
                        {
                            throw new Exception(GenerateDuplicateAddEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                        }
                    }
                    else
                    {
                        if (sourcedFromFirstShardGroup == true)
                        {
                            currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup1 = true;
                        }
                        else
                        {
                            currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup2 = true;
                        }
                    }
                    duplicatePrimaryAddEventsReceived++;

                    return false;
                }
            }
            else
            {
                if (currentElementsDictionary.ContainsKey(eventElementValue) == true)
                {
                    if (currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup1 == true && currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup2 == true)
                    {
                        if (sourcedFromFirstShardGroup == true)
                        {
                            currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup1 = false;
                        }
                        else
                        {
                            currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup2 = false;
                        }

                        return false;
                    }
                    if ((currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup1 == true && sourcedFromFirstShardGroup == false) || 
                        (currentElementsDictionary[eventElementValue].ExistsInSourceShardGroup2 == true && sourcedFromFirstShardGroup == true))
                    {
                        if (ignoreInvalidPrimaryElementEvents == true)
                        {
                            logger.Log(this, LogLevel.Error, GenerateInvalidRemoveEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                            metricLogger.Increment(new InvalidRemovePrimaryElementEventReceived());

                            return false;
                        }
                        else
                        {
                            throw new Exception(GenerateInvalidRemoveEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                        }
                    }
                    else
                    {
                        currentElementsDictionary.Remove(eventElementValue);

                        return true;
                    }
                }
                else
                {
                    if (ignoreInvalidPrimaryElementEvents == true)
                    {
                        logger.Log(this, LogLevel.Error, GenerateInvalidRemoveEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                        metricLogger.Increment(new InvalidRemovePrimaryElementEventReceived());

                        return false;
                    }
                    else
                    {
                        throw new Exception(GenerateInvalidRemoveEventMessage(sourcedFromFirstShardGroup, eventElementType, eventElementValue.ToString()));
                    }
                }
            }
        }

        #pragma warning disable 1591

        protected String GenerateDuplicateAddEventMessage(Boolean sourcedFromFirstShardGroup, String elementType, String elementValue)
        {
            String shardGroupName = "first";
            if (sourcedFromFirstShardGroup == false)
            {
                shardGroupName = "second";
            }

            return $"A duplicate 'add' {elementType} event was received from the {shardGroupName} source shard group with value '{elementValue}'.";
        }

        protected String GenerateInvalidRemoveEventMessage(Boolean sourcedFromFirstShardGroup, String elementType, String elementValue)
        {
            String shardGroupName = "first";
            if (sourcedFromFirstShardGroup == false)
            {
                shardGroupName = "second";
            }

            return $"A 'remove' {elementType} event was received from the {shardGroupName} source shard group with value '{elementValue}', where that element did not already exist.";
        }

        #pragma warning restore 1591

        #endregion

        #region Nested Classes

        /// <summary>
        /// Model/container class which tracks which source shard groups a primary element is stored in.
        /// </summary>
        protected class PrimaryElementEventSources
        {
            /// <summary>Whether the element exists in the first source shard group.</summary>
            public Boolean ExistsInSourceShardGroup1 { get; set; }

            /// <summary>Whether the element exists in the second source shard group.</summary>
            public Boolean ExistsInSourceShardGroup2 { get; set; }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.PrimaryElementEventDuplicateFilter+PrimaryElementEventSources class.
            /// </summary>
            /// <param name="existsInSourceShardGroup1">Whether the element exists in the first source shard group.</param>
            /// <param name="existsInSourceShardGroup2">Whether the element exists in the second source shard group.</param>
            public PrimaryElementEventSources(Boolean existsInSourceShardGroup1, Boolean existsInSourceShardGroup2)
            {
                this.ExistsInSourceShardGroup1 = existsInSourceShardGroup1;
                this.ExistsInSourceShardGroup2 = existsInSourceShardGroup2;
            }
        }

        #endregion
    }
}
