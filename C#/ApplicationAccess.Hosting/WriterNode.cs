﻿/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Validation;
using ApplicationAccess.Persistence;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in a multi-reader, single-writer ApplicationAccess hosting environment which allows writing permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class WriterNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>AccessManager instance which backs the event validator.</summary>
        protected ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> concurrentAccessManager;
        /// <summary>Validates events passed to the event buffer.</summary>
        protected IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator;
        /// <summary>Buffers events which change the AccessManager, writing them to the <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instance and the event cache.</summary>
        protected IAccessManagerEventBuffer<TUser, TGroup, TComponent, TAccess> eventBuffer;
        /// <summary>Flush strategy for the event buffer.</summary>
        protected IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy;
        /// <summary>Used to load the complete state of the AccessManager instance.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader;
        /// <summary>Distributes buffered events to both the <see cref="IAccessManagerTemporalEventPersister{TUser, TGroup, TComponent, TAccess}"/> instance and the event cache.</summary>
        protected AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess> eventDistributor;
        /// <summary>Persists changes to the AccessManager.</summary>
        protected IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister;
        /// <summary>Caches recent events which changed the AccessManager, so the can be accessed quickly by <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}">ReaderNodes</see></summary>
        protected IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache;        
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache
        )
        {
            this.eventBufferFlushStrategy = eventBufferFlushStrategy;
            this.persistentReader = persistentReader;
            this.eventPersister = eventPersister;
            this.eventCache = eventCache;
            concurrentAccessManager = new ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>();
            eventValidator = new ConcurrentAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>(concurrentAccessManager);
            eventDistributor = new AccessManagerTemporalEventPersisterDistributor<TUser, TGroup, TComponent, TAccess>
            (
                new List<IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>>(){ eventPersister, eventCache }
            );
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor); 
            metricLogger = new NullMetricLogger();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.WriterNode class.
        /// </summary>
        /// <param name="eventBufferFlushStrategy">Flush strategy for the <see cref="IAccessManagerEventBuffer{TUser, TGroup, TComponent, TAccess}"/> instance used by the node.</param>
        /// <param name="persistentReader">Used to load the complete state of the AccessManager instance.</param>
        /// <param name="eventPersister">Used to persist changes to the AccessManager.</param>
        /// <param name="eventCache">Cache for events which changed the AccessManager.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public WriterNode
        (
            IAccessManagerEventBufferFlushStrategy eventBufferFlushStrategy,
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader,
            IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache,
            IMetricLogger metricLogger
        )
            : this(eventBufferFlushStrategy, persistentReader, eventPersister, eventCache)
        {
            this.metricLogger = metricLogger;
            eventBuffer = new AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>(eventValidator, eventBufferFlushStrategy, eventDistributor, metricLogger);
        }

        /// <summary>
        /// Loads the latest state of the AccessManager from persistent storage.
        /// </summary>
        public void Load()
        {
            try
            {
                persistentReader.Load(concurrentAccessManager);
            }
            catch(Exception e)
            {
                throw new Exception("Failed to load access manager state from persistent storage.", e);
            }
        }

        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
            eventBuffer.AddUser(user);
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
            eventBuffer.RemoveUser(user);
        }

        /// <inheritdoc/>
        public void AddGroup(TGroup group)
        {
            eventBuffer.AddGroup(group);
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
            eventBuffer.RemoveGroup(group);
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            eventBuffer.AddUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            eventBuffer.RemoveUserToGroupMapping(user, group);
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventBuffer.AddGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            eventBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            eventBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public void AddEntityType(String entityType)
        {
            eventBuffer.AddEntityType(entityType);
        }

        /// <inheritdoc/>
        public void RemoveEntityType(String entityType)
        {
            eventBuffer.RemoveEntityType(entityType);
        }

        /// <inheritdoc/>
        public void AddEntity(String entityType, String entity)
        {
            eventBuffer.AddEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveEntity(String entityType, String entity)
        {
            eventBuffer.RemoveEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventBuffer.AddUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            eventBuffer.RemoveUserToEntityMapping(user, entityType, entity);
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventBuffer.AddGroupToEntityMapping(group, entityType, entity);
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            eventBuffer.RemoveGroupToEntityMapping(group, entityType, entity);
        }
    }
}
