/*
 * Copyright 2020 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Data;
using Microsoft.Data.SqlClient;

namespace ApplicationAccess.Persistence.SqlServer
{
    /// <summary>
    /// An implementation of ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister which persists access manager events to a Microsoft SQL Server database.
    /// </summary>
    /// <typeparam name="TUser">The validator to use to validate events.</typeparam>
    /// <typeparam name="TGroup">The strategy to use for flushing the buffers.</typeparam>
    /// <typeparam name="TComponent">The persister to use to write flushed events to permanent storage.</typeparam>
    /// <typeparam name="TAccess">The sequence number used for the last event buffered.</typeparam>
    public class SqlServerAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        // TODO: Add unit tests
        //   Add reconnect logic
        //   Add logging to log connection failures and retries



        #pragma warning disable 1591
        protected const string addUserStoredProcedureName = "AddUser";

        protected const string userParameterName = "@User";
        protected const string eventIdParameterName = "@EventId";
        protected const string transactionTimeParameterName = "@TransactionTime";
        #pragma warning restore 1591

        /// <summary>The string to use to connect to the SQL Server database.</summary>
        protected string connectionString;
        /// <summary>The connection to the SQL Server database.</summary>
        protected SqlConnection connection;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected bool disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.SqlServer.SqlServerAccessManagerTemporalEventPersister class.
        /// </summary>
        /// <param name="connectionString">The string to use to connect to the SQL Server database.</param>
        public SqlServerAccessManagerTemporalEventPersister(string connectionString)
        {
            if (String.IsNullOrWhiteSpace(connectionString) == true)
                throw new ArgumentException($"Parameter '{nameof(connectionString)}' must contain a value.");

            this.connectionString = connectionString;
            connection = new SqlConnection(connectionString);
        }

        /// <summary>
        /// Opens the connection to the SQL Server database.
        /// </summary>
        public void Connect()
        {
            try
            {
                connection.Open();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to open connection to SQL Server database.", e);
            }
        }

        /// <summary>
        /// Closes the connection to the SQL Server database.
        /// </summary>
        public void Disconnect()
        {
            try
            {
                connection.Close();
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to close connection to SQL Server database.", e);
            }
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public void AddUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public void RemoveUser(TUser user)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public void AddGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public void RemoveGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public void AddEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public void RemoveEntityType(String entityType)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public void AddEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public void RemoveEntity(String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess\ApplicationAccess.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void AddUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            // TODO 2022-05-29:
            //   Need to take stringifiers for types as parameters
            //   Need to have exception handler for strings greater than 450 chars (or whatever SQL Server limit is)

            var command = new SqlCommand(addUserStoredProcedureName, connection);
            command.CommandType = CommandType.StoredProcedure;
            //command.Parameters.AddWithValue(userParameterName, )
            try
            {
                command.ExecuteNonQuery();
            }
            catch (Exception e)
            {
                // TODO
                // Eventually needs to catch loss of connection and then retry
            }
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUser(`0,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUser(TUser user, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroup(`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToGroupMapping(`0,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToGroupMapping(`1,`1,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntityType(System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveEntity(System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveUserToEntityMapping(`0,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.AddGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.RemoveGroupToEntityMapping(`1,System.String,System.String,System.Guid,System.DateTime)"]/*'/>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerEventPersister`4.Load"]/*'/>
        public AccessManager<TUser, TGroup, TComponent, TAccess> Load()
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.Load(System.Guid)"]/*'/>
        public AccessManager<TUser, TGroup, TComponent, TAccess> Load(Guid eventId)
        {
            throw new NotImplementedException();
        }

        /// <include file='..\ApplicationAccess.Persistence\ApplicationAccess.Persistence.xml' path='doc/members/member[@name="M:ApplicationAccess.Persistence.IAccessManagerTemporalEventPersister`4.Load(System.DateTime)"]/*'/>
        public AccessManager<TUser, TGroup, TComponent, TAccess> Load(DateTime stateTime)
        {
            throw new NotImplementedException();
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the SqlServerAccessManagerTemporalEventPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591
        ~SqlServerAccessManagerTemporalEventPersister()
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
                    connection.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
