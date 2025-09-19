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
using Google.Protobuf;
using Google.Protobuf.Collections;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Hosting.Grpc.EventCache
{
    /// <summary>
    /// Converts between subclasses of <see cref="TemporalEventBufferItemBase"/> and their equivalent gRPC message.
    /// </summary>
    public class EventBufferItemToGrpcMessageConverter
    {
        /// <summary>
        /// Converts a repeated collection of <see cref="TemporalEventBufferItemListItem"/> messages to a collection of subclasses of <see cref="TemporalEventBufferItemBase"/>.
        /// </summary>
        /// <param name="eventMessages">The messages to convert.</param>
        /// <returns>The messages converted to subclasses of <see cref="TemporalEventBufferItemBase"/>.</returns>
        public IEnumerable<TemporalEventBufferItemBase> Convert(RepeatedField<TemporalEventBufferItemListItem> eventMessages)
        {
            foreach (TemporalEventBufferItemListItem currentEventMessage in eventMessages)
            {
                yield return Convert(currentEventMessage);
            }
        }

        /// <summary>
        /// Converts a collection of subclasses of <see cref="TemporalEventBufferItemBase"/> to a collection of <see cref="TemporalEventBufferItemListItem"/> messages.
        /// </summary>
        /// <param name="eventBufferItems">The events to convert.</param>
        /// <returns>The events converted to <see cref="TemporalEventBufferItemListItem"/> messages.</returns>
        public IEnumerable<TemporalEventBufferItemListItem> Convert(IEnumerable<TemporalEventBufferItemBase> eventBufferItems)
        {
            foreach (TemporalEventBufferItemBase currentEventBufferItem in eventBufferItems)
            {
                yield return Convert(currentEventBufferItem);
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Converts a <see cref="TemporalEventBufferItemListItem"/> message to a subclass of <see cref="TemporalEventBufferItemBase"/>.
        /// </summary>
        /// <param name="eventMessage">The message to convert.</param>
        /// <returns>The message converted to a subclass of <see cref="TemporalEventBufferItemBase"/>.</returns>
        protected TemporalEventBufferItemBase Convert(TemporalEventBufferItemListItem eventMessage)
        {
            switch (eventMessage.ItemCase)
            {
                case TemporalEventBufferItemListItem.ItemOneofCase.UserEventBufferItem:
                    UserEventBufferItem userEventMessage = eventMessage.UserEventBufferItem;
                    return new UserEventBufferItem<String>
                    (
                        ConvertProtobufByteStringToGuid(userEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(userEventMessage.BaseProperties.EventAction),
                        userEventMessage.User,
                        userEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        userEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.GroupEventBufferItem:
                    GroupEventBufferItem groupEventMessage = eventMessage.GroupEventBufferItem;
                    return new GroupEventBufferItem<String>
                    (
                        ConvertProtobufByteStringToGuid(groupEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(groupEventMessage.BaseProperties.EventAction),
                        groupEventMessage.Group,
                        groupEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        groupEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.UserToGroupMappingEventBufferItem:
                    UserToGroupMappingEventBufferItem userToGroupMappingEventMessage = eventMessage.UserToGroupMappingEventBufferItem;
                    return new UserToGroupMappingEventBufferItem<String, String>
                    (
                        ConvertProtobufByteStringToGuid(userToGroupMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(userToGroupMappingEventMessage.BaseProperties.EventAction),
                        userToGroupMappingEventMessage.User, 
                        userToGroupMappingEventMessage.Group,
                        userToGroupMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        userToGroupMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.GroupToGroupMappingEventBufferItem:
                    GroupToGroupMappingEventBufferItem groupToGroupMappingEventMessage = eventMessage.GroupToGroupMappingEventBufferItem;
                    return new GroupToGroupMappingEventBufferItem<String>
                    (
                        ConvertProtobufByteStringToGuid(groupToGroupMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(groupToGroupMappingEventMessage.BaseProperties.EventAction),
                        groupToGroupMappingEventMessage.FromGroup,
                        groupToGroupMappingEventMessage.ToGroup,
                        groupToGroupMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        groupToGroupMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.UserToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    UserToApplicationComponentAndAccessLevelMappingEventBufferItem userToApplicationComponentAndAccessLevelMappingEventMessage = eventMessage.UserToApplicationComponentAndAccessLevelMappingEventBufferItem;
                    return new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>
                    (
                        ConvertProtobufByteStringToGuid(userToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(userToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.EventAction),
                        userToApplicationComponentAndAccessLevelMappingEventMessage.User,
                        userToApplicationComponentAndAccessLevelMappingEventMessage.ApplicationComponent,
                        userToApplicationComponentAndAccessLevelMappingEventMessage.AccessLevel,
                        userToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        userToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.GroupToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    GroupToApplicationComponentAndAccessLevelMappingEventBufferItem groupToApplicationComponentAndAccessLevelMappingEventMessage = eventMessage.GroupToApplicationComponentAndAccessLevelMappingEventBufferItem;
                    return new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String>
                    (
                        ConvertProtobufByteStringToGuid(groupToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(groupToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.EventAction),
                        groupToApplicationComponentAndAccessLevelMappingEventMessage.Group,
                        groupToApplicationComponentAndAccessLevelMappingEventMessage.ApplicationComponent,
                        groupToApplicationComponentAndAccessLevelMappingEventMessage.AccessLevel,
                        groupToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        groupToApplicationComponentAndAccessLevelMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.UserToEntityMappingEventBufferItem:
                    UserToEntityMappingEventBufferItem userToEntityMappingEventMessage = eventMessage.UserToEntityMappingEventBufferItem;
                    return new UserToEntityMappingEventBufferItem<String>
                    (
                        ConvertProtobufByteStringToGuid(userToEntityMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(userToEntityMappingEventMessage.BaseProperties.EventAction),
                        userToEntityMappingEventMessage.User,
                        userToEntityMappingEventMessage.EntityType,
                        userToEntityMappingEventMessage.Entity,
                        userToEntityMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        userToEntityMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.GroupToEntityMappingEventBufferItem:
                    GroupToEntityMappingEventBufferItem groupToEntityMappingEventMessage = eventMessage.GroupToEntityMappingEventBufferItem;
                    return new GroupToEntityMappingEventBufferItem<String>
                    (
                        ConvertProtobufByteStringToGuid(groupToEntityMappingEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(groupToEntityMappingEventMessage.BaseProperties.EventAction),
                        groupToEntityMappingEventMessage.Group,
                        groupToEntityMappingEventMessage.EntityType,
                        groupToEntityMappingEventMessage.Entity,
                        groupToEntityMappingEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        groupToEntityMappingEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.EntityEventBufferItem:
                    ApplicationAccess.Hosting.Grpc.Models.EntityEventBufferItem entityEventMessage = eventMessage.EntityEventBufferItem;
                    return new ApplicationAccess.Persistence.Models.EntityEventBufferItem
                    (
                        ConvertProtobufByteStringToGuid(entityEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(entityEventMessage.BaseProperties.EventAction),
                        entityEventMessage.EntityType,
                        entityEventMessage.Entity,
                        entityEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        entityEventMessage.BaseProperties.HashCode
                    );

                case TemporalEventBufferItemListItem.ItemOneofCase.EntityTypeEventBufferItem:
                    ApplicationAccess.Hosting.Grpc.Models.EntityTypeEventBufferItem entityTypeEventMessage = eventMessage.EntityTypeEventBufferItem;
                    return new ApplicationAccess.Persistence.Models.EntityTypeEventBufferItem
                    (
                        ConvertProtobufByteStringToGuid(entityTypeEventMessage.BaseProperties.EventId),
                        ConvertGrpcEventAction(entityTypeEventMessage.BaseProperties.EventAction),
                        entityTypeEventMessage.EntityType,
                        entityTypeEventMessage.BaseProperties.OccurredTime.ToDateTime(),
                        entityTypeEventMessage.BaseProperties.HashCode
                    );

                default:
                    throw new Exception($"Encountered unhandled event gRPC message type '{eventMessage.GetType().FullName}.");
            }
        }

        /// <summary>
        /// Converts a subclass of <see cref="TemporalEventBufferItemBase"/> to a <see cref="TemporalEventBufferItemListItem"/> message.
        /// </summary>
        /// <param name="eventBufferItem">The event to convert.</param>
        /// <returns>The event converted to a <see cref="TemporalEventBufferItemListItem"/> message.</returns>
        protected TemporalEventBufferItemListItem Convert(TemporalEventBufferItemBase eventBufferItem)
        {
            TemporalEventBufferItemListItem returnTemporalEventBufferItemListItem;
            switch (eventBufferItem)
            {
                case UserEventBufferItem<String> userEventBufferItem:
                    UserEventBufferItem returnUserEventBufferItem = new();
                    returnUserEventBufferItem.BaseProperties = CreateBaseProperties(userEventBufferItem);
                    returnUserEventBufferItem.User = userEventBufferItem.User;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.UserEventBufferItem = returnUserEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case GroupEventBufferItem<String> groupEventBufferItem:
                    GroupEventBufferItem returnGroupEventBufferItem = new();
                    returnGroupEventBufferItem.BaseProperties = CreateBaseProperties(groupEventBufferItem);
                    returnGroupEventBufferItem.Group = groupEventBufferItem.Group;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.GroupEventBufferItem = returnGroupEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case UserToGroupMappingEventBufferItem<String, String> userToGroupMappingEventBufferItem:
                    UserToGroupMappingEventBufferItem returnUserToGroupMappingEventBufferItem = new();
                    returnUserToGroupMappingEventBufferItem.BaseProperties = CreateBaseProperties(userToGroupMappingEventBufferItem);
                    returnUserToGroupMappingEventBufferItem.User = userToGroupMappingEventBufferItem.User;
                    returnUserToGroupMappingEventBufferItem.Group = userToGroupMappingEventBufferItem.Group;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.UserToGroupMappingEventBufferItem = returnUserToGroupMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case GroupToGroupMappingEventBufferItem<String> groupToGroupMappingEventBufferItem:
                    GroupToGroupMappingEventBufferItem returnGroupToGroupMappingEventBufferItem = new();
                    returnGroupToGroupMappingEventBufferItem.BaseProperties = CreateBaseProperties(groupToGroupMappingEventBufferItem);
                    returnGroupToGroupMappingEventBufferItem.FromGroup = groupToGroupMappingEventBufferItem.FromGroup;
                    returnGroupToGroupMappingEventBufferItem.ToGroup = groupToGroupMappingEventBufferItem.ToGroup;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.GroupToGroupMappingEventBufferItem = returnGroupToGroupMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String> userToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    UserToApplicationComponentAndAccessLevelMappingEventBufferItem returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem = new();
                    returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem.BaseProperties = CreateBaseProperties(userToApplicationComponentAndAccessLevelMappingEventBufferItem);
                    returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem.User = userToApplicationComponentAndAccessLevelMappingEventBufferItem.User;
                    returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent = userToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent;
                    returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel = userToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.UserToApplicationComponentAndAccessLevelMappingEventBufferItem = returnUserToApplicationComponentAndAccessLevelMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, String, String> groupToApplicationComponentAndAccessLevelMappingEventBufferItem:
                    GroupToApplicationComponentAndAccessLevelMappingEventBufferItem returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem = new();
                    returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.BaseProperties = CreateBaseProperties(groupToApplicationComponentAndAccessLevelMappingEventBufferItem);
                    returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.Group = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.Group;
                    returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.ApplicationComponent;
                    returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel = groupToApplicationComponentAndAccessLevelMappingEventBufferItem.AccessLevel;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.GroupToApplicationComponentAndAccessLevelMappingEventBufferItem = returnGroupToApplicationComponentAndAccessLevelMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case UserToEntityMappingEventBufferItem<String> userToEntityMappingEventBufferItem:
                    UserToEntityMappingEventBufferItem returnUserToEntityMappingEventBufferItem = new();
                    returnUserToEntityMappingEventBufferItem.BaseProperties = CreateBaseProperties(userToEntityMappingEventBufferItem);
                    returnUserToEntityMappingEventBufferItem.User = userToEntityMappingEventBufferItem.User;
                    returnUserToEntityMappingEventBufferItem.EntityType = userToEntityMappingEventBufferItem.EntityType;
                    returnUserToEntityMappingEventBufferItem.Entity = userToEntityMappingEventBufferItem.Entity;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.UserToEntityMappingEventBufferItem = returnUserToEntityMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case GroupToEntityMappingEventBufferItem<String> groupToEntityMappingEventBufferItem:
                    GroupToEntityMappingEventBufferItem returnGroupToEntityMappingEventBufferItem = new();
                    returnGroupToEntityMappingEventBufferItem.BaseProperties = CreateBaseProperties(groupToEntityMappingEventBufferItem);
                    returnGroupToEntityMappingEventBufferItem.Group = groupToEntityMappingEventBufferItem.Group;
                    returnGroupToEntityMappingEventBufferItem.EntityType = groupToEntityMappingEventBufferItem.EntityType;
                    returnGroupToEntityMappingEventBufferItem.Entity = groupToEntityMappingEventBufferItem.Entity;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.GroupToEntityMappingEventBufferItem = returnGroupToEntityMappingEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case ApplicationAccess.Persistence.Models.EntityEventBufferItem entityEventBufferItem:
                    ApplicationAccess.Hosting.Grpc.Models.EntityEventBufferItem returnEntityEventBufferItem = new();
                    returnEntityEventBufferItem.BaseProperties = CreateBaseProperties(entityEventBufferItem);
                    returnEntityEventBufferItem.EntityType = entityEventBufferItem.EntityType;
                    returnEntityEventBufferItem.Entity = entityEventBufferItem.Entity;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.EntityEventBufferItem = returnEntityEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                case ApplicationAccess.Persistence.Models.EntityTypeEventBufferItem entityTypeEventBufferItem:
                    ApplicationAccess.Hosting.Grpc.Models.EntityTypeEventBufferItem returnEntityTypeEventBufferItem = new();
                    returnEntityTypeEventBufferItem.BaseProperties = CreateBaseProperties(entityTypeEventBufferItem);
                    returnEntityTypeEventBufferItem.EntityType = entityTypeEventBufferItem.EntityType;
                    returnTemporalEventBufferItemListItem = new();
                    returnTemporalEventBufferItemListItem.EntityTypeEventBufferItem = returnEntityTypeEventBufferItem;
                    return returnTemporalEventBufferItemListItem;

                default:
                    throw new Exception($"Encountered unhandled event type '{eventBufferItem.GetType().FullName}.");
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="TemporalEventBufferItem"> to use as the 'baseProperties' field in a <see cref="TemporalEventBufferItemListItem"/>.
        /// </summary>
        /// <param name="eventBufferItem">The event to create the base properties from.</param>
        /// <param name="eventAction">The action of the event.</param>
        /// <param name="occurredTime">The time that the event originally occurred.</param>
        /// <param name="hashCode">The hash code for the key primary element of the event.</param>
        /// <returns>The <see cref="TemporalEventBufferItem">.</returns>
        protected TemporalEventBufferItem CreateBaseProperties(TemporalEventBufferItemBase eventBufferItem)
        {
            TemporalEventBufferItem baseProperties = new();
            baseProperties.EventId = ConvertGuidToProtobufByteString(eventBufferItem.EventId);
            baseProperties.EventAction = ConvertEventAction(eventBufferItem.EventAction);
            baseProperties.OccurredTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(eventBufferItem.OccurredTime);
            baseProperties.HashCode = eventBufferItem.HashCode;

            return baseProperties;
        }

        #pragma warning disable 1591

        protected Guid ConvertProtobufByteStringToGuid(ByteString inputByteString)
        {
            try
            {
                return new Guid(inputByteString.ToByteArray());
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to convert Protobuf {typeof(ByteString).Name} to a {typeof(Guid).Name}.");
            }
        }

        protected ByteString ConvertGuidToProtobufByteString(Guid inputGuid)
        {
            try
            {
                return ByteString.CopyFrom(inputGuid.ToByteArray());
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to convert {typeof(Guid).Name} '{inputGuid.ToString()}' to a Protobuf {typeof(ByteString).Name}.");
            }
        }

        protected ApplicationAccess.Persistence.Models.EventAction ConvertGrpcEventAction(ApplicationAccess.Hosting.Grpc.Models.EventAction inputEventAction)
        {
            try
            {
                return Enum.Parse<ApplicationAccess.Persistence.Models.EventAction>(inputEventAction.ToString());
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to convert gRPC {typeof(ApplicationAccess.Hosting.Grpc.Models.EventAction).Name} enum '{inputEventAction.ToString()}' into an {typeof(ApplicationAccess.Hosting.Grpc.Models.EventAction).FullName}.", nameof(inputEventAction), e);
            }
        }

        protected ApplicationAccess.Hosting.Grpc.Models.EventAction ConvertEventAction(ApplicationAccess.Persistence.Models.EventAction inputEventAction)
        {
            try
            {
                return Enum.Parse<ApplicationAccess.Hosting.Grpc.Models.EventAction>(inputEventAction.ToString());
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Failed to convert {typeof(ApplicationAccess.Hosting.Grpc.Models.EventAction).Name} enum '{inputEventAction.ToString()}' into a gRPC {typeof(ApplicationAccess.Hosting.Grpc.Models.EventAction).FullName}.", nameof(inputEventAction), e);
            }
        }

        #pragma warning restore 1591

        #endregion
    }
}
