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
using System.Text;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Processes a collecion of <see cref="EventBufferItemBase"/> objects, applying the changes to an <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> instance.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class AccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> instance to apply the events to.</summary>
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessorInstance;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerEventProcessor class.
        /// </summary>
        /// <param name="eventProcessorInstance">The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> instance to apply the events to.</param>
        public AccessManagerEventProcessor(IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessorInstance)
        {
            this.eventProcessorInstance = eventProcessorInstance;
        }

        /// <summary>
        /// Processes the specified collecion of event objects, applying the changes to the AccessManager instance.
        /// </summary>
        /// <param name="events">The events to process/apply.</param>
        public void Process(IEnumerable<EventBufferItemBase> events)
        {
            foreach (EventBufferItemBase currentEvent in events)
            {
                Process(currentEvent);
            }
        }

        /// <summary>
        /// Processes the specified event object, applying the changes to the AccessManager instance.
        /// </summary>
        /// <param name="eventBufferItem">The event to process/apply.</param>
        public void Process(EventBufferItemBase eventBufferItem)
        {
            switch (eventBufferItem)
            {
                case UserEventBufferItem<TUser> userEventBufferItem:
                    if (userEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddUser(userEventBufferItem.User);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveUser(userEventBufferItem.User);
                    }
                    break;

                case GroupEventBufferItem<TGroup> groupEventBufferItem:
                    if (groupEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddGroup(groupEventBufferItem.Group);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveGroup(groupEventBufferItem.Group);
                    }
                    break;

                case UserToGroupMappingEventBufferItem<TUser, TGroup> userToGroupMappingEventBufferItem:
                    if (userToGroupMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddUserToGroupMapping(userToGroupMappingEventBufferItem.User, userToGroupMappingEventBufferItem.Group);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveUserToGroupMapping(userToGroupMappingEventBufferItem.User, userToGroupMappingEventBufferItem.Group);
                    }
                    break;

                case GroupToGroupMappingEventBufferItem<TGroup> groupToGroupMappingEventBufferItem:
                    if (groupToGroupMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddGroupToGroupMapping(groupToGroupMappingEventBufferItem.FromGroup, groupToGroupMappingEventBufferItem.ToGroup);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveGroupToGroupMapping(groupToGroupMappingEventBufferItem.FromGroup, groupToGroupMappingEventBufferItem.ToGroup);
                    }
                    break;

                case UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess> userToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    TUser user = userToApplicationComponentAndAccessLevelMappingEventBufferItem.User;
                    TComponent applicationComponent = userToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent;
                    TAccess accessLevel = userToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel;
                    if (userToApplicationComponentAndAccessLevelMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
                    }
                    break;

                case GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess> groupToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    TGroup group = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.Group;
                    applicationComponent = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent;
                    accessLevel = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel;
                    if (groupToApplicationComponentAndAccessLevelMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
                    }
                    break;

                case UserToEntityMappingEventBufferItem<TUser> userToEntityMappingEventBufferItem:
                    user = userToEntityMappingEventBufferItem.User;
                    String entityType = userToEntityMappingEventBufferItem.EntityType;
                    String entity = userToEntityMappingEventBufferItem.Entity;
                    if (userToEntityMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddUserToEntityMapping(user, entityType, entity);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveUserToEntityMapping(user, entityType, entity);
                    }
                    break;

                case GroupToEntityMappingEventBufferItem<TGroup> groupToEntityMappingEventBufferItem:
                    group = groupToEntityMappingEventBufferItem.Group;
                    entityType = groupToEntityMappingEventBufferItem.EntityType;
                    entity = groupToEntityMappingEventBufferItem.Entity;
                    if (groupToEntityMappingEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddGroupToEntityMapping(group, entityType, entity);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveGroupToEntityMapping(group, entityType, entity);
                    }
                    break;

                case EntityEventBufferItem entityEventBufferItem:
                    if (entityEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddEntity(entityEventBufferItem.EntityType, entityEventBufferItem.Entity);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveEntity(entityEventBufferItem.EntityType, entityEventBufferItem.Entity);
                    }
                    break;

                case EntityTypeEventBufferItem entityTypeEventBufferItem:
                    if (entityTypeEventBufferItem.EventAction == EventAction.Add)
                    {
                        eventProcessorInstance.AddEntityType(entityTypeEventBufferItem.EntityType);
                    }
                    else
                    {
                        eventProcessorInstance.RemoveEntityType(entityTypeEventBufferItem.EntityType);
                    }
                    break;

                default:
                    throw new Exception($"Encountered unhandled event type '{eventBufferItem.GetType().FullName}.");
            }
        }
    }
}
