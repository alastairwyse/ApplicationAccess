/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApplicationAccess.Persistence;
using ApplicationAccess.Hosting.Rest.Client;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Distributes methods calls syncronously to multiple <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> instances.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManagers.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManagers.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManagers.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerClientDistributor<TUser, TGroup, TComponent, TAccess> : IAccessManager<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        // TODO: Only methods from IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> are current implemented

        /// <summary>Holds the <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> instances to distribute to.</summary>
        protected List<AccessManagerClient<TUser, TGroup, TComponent, TAccess>> clients;        
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <inheritdoc />
        public IEnumerable<TUser> Users => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<TGroup> Groups => throw new NotImplementedException();

        /// <inheritdoc />
        public IEnumerable<String> EntityTypes => throw new NotImplementedException();

        public AccessManagerClientDistributor(IEnumerable<AccessManagerClient<TUser, TGroup, TComponent, TAccess>> clients)
        {
            this.clients = new List<AccessManagerClient<TUser, TGroup, TComponent, TAccess>>();
            foreach (AccessManagerClient<TUser, TGroup, TComponent, TAccess> currentClient in clients)
            {
                this.clients.Add(currentClient);
            }
            disposed = false;
        }

        /// <inheritdoc />
        public void AddUser(TUser user)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) => 
            { 
                client.AddUser(user); 
            });
        }

        /// <inheritdoc />
        public Boolean ContainsUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveUser(TUser user)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveUser(user);
            });
        }

        /// <inheritdoc />
        public void AddGroup(TGroup group)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddGroup(group);
            });
        }

        /// <inheritdoc />
        public Boolean ContainsGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveGroup(TGroup group)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveGroup(group);
            });
        }

        /// <inheritdoc />
        public void AddEntityType(String entityType)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddEntityType(entityType);
            });
        }

        /// <inheritdoc />
        public Boolean ContainsEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveEntityType(String entityType)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveEntityType(entityType);
            });
        }

        /// <inheritdoc />
        public void AddEntity(String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddEntity(entityType, entity);
            });
        }

        /// <inheritdoc />
        public Boolean ContainsEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<String> GetEntities(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveEntity(String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveEntity(entityType, entity);
            });
        }

        /// <inheritdoc />
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddUserToGroupMapping(user, group);
            });
        }

        /// <inheritdoc />
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveUserToGroupMapping(user, group);
            });
        }

        /// <inheritdoc />
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddGroupToGroupMapping(fromGroup, toGroup);
            });
        }

        /// <inheritdoc />
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveGroupToGroupMapping(fromGroup, toGroup);
            });
        }

        /// <inheritdoc />
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            });
        }

        /// <inheritdoc />
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
            });
        }

        /// <inheritdoc />
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            });
        }

        /// <inheritdoc />
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
            });
        }

        /// <inheritdoc />
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddUserToEntityMapping(user, entityType, entity);
            });
        }

        /// <inheritdoc />
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveUserToEntityMapping(user, entityType, entity);
            });
        }

        /// <inheritdoc />
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.AddGroupToEntityMapping(group, entityType, entity);
            });
        }

        /// <inheritdoc />
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            InvokeAgainstAllClients((IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> client) =>
            {
                client.RemoveGroupToEntityMapping(group, entityType, entity);
            });
        }

        /// <inheritdoc />
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        protected void InvokeAgainstAllClients(Action<IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>> clientAction)
        {
            var exceptions = new List<Exception>();
            foreach (IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> currentClient in clients)
            {
                try
                {
                    clientAction(currentClient);
                }
                catch(Exception e)
                {
                    exceptions.Add(e);
                }
            }
            if (exceptions.Count > 0)
            {
                throw new AggregateException(exceptions);
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the AccessManagerClientDistributor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerClientDistributor()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(Boolean disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    foreach (AccessManagerClient<TUser, TGroup, TComponent, TAccess> currentClient in clients)
                    {
                        currentClient.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
