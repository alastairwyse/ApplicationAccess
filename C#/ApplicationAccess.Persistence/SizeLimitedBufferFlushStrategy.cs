/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// A buffer flush strategy that flushes/processes the buffers when the total number of buffered items reaches a pre-defined limit.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class SizeLimitedBufferFlushStrategy<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess>
    {
        public event EventHandler BufferFlushed;


        public void AddUser(TUser user)
        {
            throw new NotImplementedException();
        }

        public void RemoveUser(TUser user)
        {
            throw new NotImplementedException();
        }

        public void AddGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        public void RemoveGroup(TGroup group)
        {
            throw new NotImplementedException();
        }

        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            throw new NotImplementedException();
        }

        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            throw new NotImplementedException();
        }

        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            throw new NotImplementedException();
        }

        public void AddEntityType(string entityType)
        {
            throw new NotImplementedException();
        }

        public void RemoveEntityType(string entityType)
        {
            throw new NotImplementedException();
        }

        public void AddEntity(string entityType, string entity)
        {
            throw new NotImplementedException();
        }

        public void RemoveEntity(string entityType, string entity)
        {
            throw new NotImplementedException();
        }

        public void AddUserToEntityMapping(TUser user, string entityType, string entity)
        {
            throw new NotImplementedException();
        }

        public void RemoveUserToEntityMapping(TUser user, string entityType, string entity)
        {
            throw new NotImplementedException();
        }

        public void AddGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            throw new NotImplementedException();
        }

        public void RemoveGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
            throw new NotImplementedException();
        }
    }
}
