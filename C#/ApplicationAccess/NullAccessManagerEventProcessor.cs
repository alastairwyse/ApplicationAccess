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

namespace ApplicationAccess
{
    /// <summary>
    /// Implementation of <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> which does not process any events.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <inheritdoc/>
        public void AddUser(TUser user)
        {
        }

        /// <inheritdoc/>
        public void RemoveUser(TUser user)
        {
        }

        /// <inheritdoc/>
        public virtual void AddGroup(TGroup group)
        {
        }

        /// <inheritdoc/>
        public void RemoveGroup(TGroup group)
        {
        }

        /// <inheritdoc/>
        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
        }

        /// <inheritdoc/>
        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
        }

        /// <inheritdoc/>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
        }

        /// <inheritdoc/>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
        }

        /// <inheritdoc/>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
        }

        /// <inheritdoc/>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
        }

        /// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
        }

        /// <inheritdoc/>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
        }

        /// <inheritdoc/>
        public void AddEntityType(string entityType)
        {
        }

        /// <inheritdoc/>
        public void RemoveEntityType(string entityType)
        {
        }

        /// <inheritdoc/>
        public void AddEntity(string entityType, string entity)
        {
        }

        /// <inheritdoc/>
        public void RemoveEntity(string entityType, string entity)
        {
        }

        /// <inheritdoc/>
        public void AddUserToEntityMapping(TUser user, string entityType, string entity)
        {
        }

        /// <inheritdoc/>
        public void RemoveUserToEntityMapping(TUser user, string entityType, string entity)
        {
        }

        /// <inheritdoc/>
        public void AddGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
        }

        /// <inheritdoc/>
        public void RemoveGroupToEntityMapping(TGroup group, string entityType, string entity)
        {
        }
    }
}
