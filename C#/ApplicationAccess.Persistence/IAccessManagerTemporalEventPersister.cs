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
    /// Defines methods to write events which change the structure of an AccessManager class to persistent storage.  Designed to operate behind a component which buffers the events, and hence methods include a unique id for each event, and the date/time in the past when the even occurred.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the user was added.</param>
        void AddUser(TUser user, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the user was removed.</param>
        void RemoveUser(TUser user, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the group was added.</param>
        void AddGroup(TGroup group, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the group was removed.</param>
        void RemoveGroup(TGroup group, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the user to group mapping was added.</param>
        void AddUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the user to group mapping was removed.</param>
        void RemoveUserToGroupMapping(TUser user, TGroup group, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping between the groups was added.</param>
        void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping between the groups was removed.</param>
        void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was added.</param>
        void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was removed.</param>
        void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was added.</param>
        void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was removed.</param>
        void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the entity type was added.</param>
        void AddEntityType(String entityType, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the entity type was removed.</param>
        void RemoveEntityType(String entityType, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the entity was added.</param>
        void AddEntity(String entityType, String entity, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the entity was removed.</param>
        void RemoveEntity(String entityType, String entity, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was added.</param>
        void AddUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was removed.</param>
        void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was added.</param>
        void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime);

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="eventId">The unique id of the event.</param>
        /// <param name="occurredTime">The historic date and time that the mapping was removed.</param>
        void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Guid eventId, DateTime occurredTime);
    }
}
