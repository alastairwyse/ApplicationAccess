/*
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
using System.Threading;
using ApplicationAccess.Persistence;

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// A node in an ApplicationAccess hosting environment which allows reading permissions and authorizations for an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class ReaderNode<TUser, TGroup, TComponent, TAccess> : IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        /// <summary>The strategy/methodology to use to refresh the contents of the reader node.</summary>
        protected IReaderNodeRefreshStrategy refreshStrategy;
        /// <summary>Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</summary>
        protected IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache;
        /// <summary>Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader;
        /// <summary>The AccessManager which stores the permissions and authorizations for the application.</summary>
        protected AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager;
        /// <summary>The id of the most recent event which changed the AccessManager.</summary>
        protected Guid latestEventId;
        /// <summary>The delegate which handles an <see cref="IReaderNodeRefreshStrategy.ReaderNodeRefreshed">ReaderNodeRefreshed</see> event.</summary>
        protected EventHandler refreshedEventHandler;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.ReaderNode class.
        /// </summary>
        /// <param name="refreshStrategy">The strategy/methodology to use to refresh the contents of the reader node.</param>
        /// <param name="eventCache">Cache for events which change the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted.</param>
        /// <param name="persistentReader">Reader which allows retriving the complete state of the <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> being hosted from persistent storage.</param>
        public ReaderNode(IReaderNodeRefreshStrategy refreshStrategy, IAccessManagerTemporalEventCache<TUser, TGroup, TComponent, TAccess> eventCache, IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> persistentReader)
        {
            this.refreshStrategy = refreshStrategy;
            this.eventCache = eventCache;
            this.persistentReader = persistentReader;
            accessManager = new ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>();
            // Subscribe to the refreshStrategy's 'ReaderNodeRefreshed' event
            refreshedEventHandler = (Object sender, EventArgs e) => { Refresh(); };
            refreshStrategy.ReaderNodeRefreshed += refreshedEventHandler;
            disposed = false;
            // TODO: Are metrics going to be captured inside this class, or in an AccessManagerQueryProcessorMetricLogger wrapping this?
            // TODO: Mechanism to rethrow worked thread exceptions on main thread
            // TODO: Need some sort of base 'FatalOperationException' or similar to allow surrounding host to figure out when something fatal has happened
            // TODO: Specific metrics to capture
            //    Instances of cache misses
            //    Time to perform load
            //    Number of latest events received
            //    Difference between event time and current time
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TUser> Users
        {
            get
            {
                refreshStrategy.NotifyQueryMethodCalled();
                return accessManager.Users;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                refreshStrategy.NotifyQueryMethodCalled();
                return accessManager.Groups;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                refreshStrategy.NotifyQueryMethodCalled();
                return accessManager.EntityTypes;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsUser(TUser user)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.ContainsUser(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsGroup(TGroup group)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.ContainsGroup(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetUserToGroupMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetGroupToGroupMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsEntityType(String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.ContainsEntityType(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetEntities(String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetEntities(entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.ContainsEntity(entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetUserToEntityMappings(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetUserToEntityMappings(user, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetGroupToEntityMappings(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetGroupToEntityMappings(group, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToApplicationComponent(`0,`2,`3)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToEntity(`0,System.String,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.HasAccessToEntity(user, entityType, entity);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetApplicationComponentsAccessibleByUser(`0)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetApplicationComponentsAccessibleByUser(user);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetApplicationComponentsAccessibleByGroup(`1)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetApplicationComponentsAccessibleByGroup(group);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntitiesAccessibleByUser(`0,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetEntitiesAccessibleByUser(user, entityType);
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntitiesAccessibleByGroup(`1,System.String)"]/*'/>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            refreshStrategy.NotifyQueryMethodCalled();
            return accessManager.GetEntitiesAccessibleByGroup(group, entityType);
        }

        /// <summary>
        /// Loads all permissions and authorizations from persistent storage.
        /// </summary>
        public void Load()
        {
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> newAccessManager = new ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>();
            try
            {
                Tuple<Guid, DateTime> state = persistentReader.Load(newAccessManager);
                latestEventId = state.Item1;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load access manager state from persistent storage.", e);
            }
            Interlocked.Exchange(ref accessManager, newAccessManager);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Refreshes/updates the contents of the AccessManager by first attempting to retrieve any events occurring after the one stored in 'latestEventId' and applying them to the AccessManager instance, or by reading the entire latest state from persistent storage if the event retrieval fails.
        /// </summary>
        protected void Refresh()
        {
            IList<EventBufferItemBase> updateEvents = null;
            try
            {
                updateEvents = eventCache.GetAllEventsSince(latestEventId);
            }
            catch (EventNotCachedException)
            {
                try
                {
                    Load();
                }
                catch (Exception e)
                {
                    throw new ReaderNodeRefreshException($"Failed to refresh the entire contents of the reader node.", e);
                }
            }
            catch (Exception e)
            {
                throw new ReaderNodeRefreshException($"Failed to retrieve latest access manager events following event '{latestEventId}' from cache.", e);
            }

            if (updateEvents != null)
            {
                var eventProcessor = new AccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>(accessManager);
                eventProcessor.Process(updateEvents);
                latestEventId = updateEvents[updateEvents.Count - 1].EventId;
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the ReaderNode.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~ReaderNode()
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
                    refreshStrategy.ReaderNodeRefreshed -= refreshedEventHandler;
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
