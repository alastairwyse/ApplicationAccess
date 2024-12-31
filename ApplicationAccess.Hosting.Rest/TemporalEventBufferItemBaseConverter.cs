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
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Serialization;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Subclass of <see cref="JsonConverter{T}"/> for converting subclasses of <see cref="TemporalEventBufferItemBase"/> to and from JSON.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager the events were created by.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager the events were created by.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager the events were created by.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class TemporalEventBufferItemBaseConverter<TUser, TGroup, TComponent, TAccess> : JsonConverter<TemporalEventBufferItemBase>
    {
        // TODO: Possibly better to move to another project... possibly ApplicationAccess.Hosting.Rest.Utilities or ApplicationAccess.Serialization if either are ever changed to .NET 6.0+

        protected const String EventIdPropertyName = "eventId";
        protected const String EventActionPropertyName = "eventAction";
        protected const String OccurredTimePropertyName = "occurredTime";
        protected const String HashCodePropertyName = "hashCode";
        protected const String EntityTypePropertyName = "entityType";
        protected const String EntityPropertyName = "entity";
        protected const String UserPropertyName = "user";
        protected const String GroupPropertyName = "group";
        protected const String ApplicationComponentPropertyName = "applicationComponent";
        protected const String AccessLevelPropertyName = "accessLevel";
        protected const String FromGroupPropertyName = "fromGroup";
        protected const String ToGroupPropertyName = "toGroup";
        protected const String dateTimeFormatString = "yyyy-MM-dd HH:mm:ss.fffffff";

        /// <summary>A string converter for users.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.TemporalEventBufferItemBaseConverter class.
        /// </summary>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        public TemporalEventBufferItemBaseConverter
        (
            IUniqueStringifier<TUser> userStringifier, 
            IUniqueStringifier<TGroup> groupStringifier, 
            IUniqueStringifier<TComponent> applicationComponentStringifier, 
            IUniqueStringifier<TAccess> accessLevelStringifier
        )
        {
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
        }

        /// <inheritdoc/>
        public override Boolean CanConvert(Type typeToConvert)
        {
            return typeof(TemporalEventBufferItemBase).IsAssignableFrom(typeToConvert);
        }
        
        /// <inheritdoc/>
        public override TemporalEventBufferItemBase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // TODO: Move some parts into protected methods to reduce nesting

            // Read TemporalEventBufferItemBase properties
            Tuple<Guid, EventAction, DateTime, Int32> baseProperties = ReadTemporalEventBufferItemBaseProperties(ref reader);
            Guid eventId = baseProperties.Item1;
            EventAction eventAction = baseProperties.Item2;
            DateTime occurredTime = baseProperties.Item3;
            Int32 hashcode = baseProperties.Item4;

            reader.Read();
            ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.PropertyName);
            String nextPropertyName = reader.GetString();

            // Handle EntityTypeEventBufferItem or a subclass
            if (nextPropertyName == EntityTypePropertyName)
            {
                String entityType = ReadStringPropertyValue(ref reader);
                reader.Read();
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    nextPropertyName = reader.GetString();
                    // Handle EntityEventBufferItem or a subclass
                    if (nextPropertyName == EntityPropertyName)
                    {
                        String entity = ReadStringPropertyValue(ref reader);
                        reader.Read();
                        if (reader.TokenType == JsonTokenType.PropertyName)
                        {
                            nextPropertyName = reader.GetString();
                            // Handle UserToEntityMappingEventBufferItem
                            if (nextPropertyName == UserPropertyName)
                            {
                                String stringifiedUser = ReadStringPropertyValue(ref reader);
                                reader.Read();
                                ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                                return new UserToEntityMappingEventBufferItem<TUser>(eventId, eventAction, userStringifier.FromString(stringifiedUser), entityType, entity, occurredTime, hashcode);
                            }
                            // Handle GroupToEntityMappingEventBufferItem
                            if (nextPropertyName == GroupPropertyName)
                            {
                                String stringifiedGroup = ReadStringPropertyValue(ref reader);
                                reader.Read();
                                ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                                return new GroupToEntityMappingEventBufferItem<TGroup>(eventId, eventAction, groupStringifier.FromString(stringifiedGroup), entityType, entity, occurredTime, hashcode);
                            }
                            else
                            {
                                throw new DeserializationException($"Encountered unhandled JSON property '{nextPropertyName}' when attempting to deserialize {typeof(EntityEventBufferItem).Name} subclass.");
                            }
                        }
                        else
                        {
                            ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                            return new EntityEventBufferItem(eventId, eventAction, entityType, entity, occurredTime, hashcode);
                        }
                    }
                    else
                    {
                        throw new DeserializationException($"Encountered unhandled JSON property '{nextPropertyName}' when attempting to deserialize {typeof(EntityTypeEventBufferItem).Name} subclass.");
                    }
                }
                else
                {
                    ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                    return new EntityTypeEventBufferItem(eventId, eventAction, entityType, occurredTime, hashcode);
                }
            }
            // Handle documents with a 'user' property
            else if (nextPropertyName == UserPropertyName)
            {
                String userAsString = ReadStringPropertyValue(ref reader);
                TUser user = userStringifier.FromString(userAsString);
                reader.Read();
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    nextPropertyName = reader.GetString();
                    // Handle UserToApplicationComponentAndAccessLevelMappingEventBufferItem
                    if (nextPropertyName == ApplicationComponentPropertyName)
                    {
                        String applicationComponentAsString = ReadStringPropertyValue(ref reader);
                        TComponent applicationComponent = applicationComponentStringifier.FromString(applicationComponentAsString);
                        ReadPropertyName(ref reader, AccessLevelPropertyName);
                        String accessLevelAsString = ReadStringPropertyValue(ref reader);
                        TAccess accessLevel = accessLevelStringifier.FromString(accessLevelAsString);
                        reader.Read();
                        ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                        return new UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>(eventId, eventAction, user, applicationComponent, accessLevel, occurredTime, hashcode);
                    }
                    // Handle UserToGroupMappingEventBufferItem
                    else if (nextPropertyName == GroupPropertyName)
                    {
                        String groupAsString = ReadStringPropertyValue(ref reader);
                        TGroup group = groupStringifier.FromString(groupAsString);
                        reader.Read();
                        ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                        return new UserToGroupMappingEventBufferItem<TUser, TGroup>(eventId, eventAction, user, group, occurredTime, hashcode);
                    }
                    else
                    {
                        throw new DeserializationException($"Encountered unhandled JSON property '{nextPropertyName}' when attempting to deserialize {typeof(UserEventBufferItem<TUser>).Name} subclass.");
                    }
                }
                else
                {
                    ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                    return new UserEventBufferItem<TUser>(eventId, eventAction, user, occurredTime, hashcode);
                }
            }
            // Handle documents with a 'group' property
            else if (nextPropertyName == GroupPropertyName)
            {
                String groupAsString = ReadStringPropertyValue(ref reader);
                TGroup group = groupStringifier.FromString(groupAsString);
                reader.Read();
                if (reader.TokenType == JsonTokenType.PropertyName)
                {
                    nextPropertyName = reader.GetString();
                    // Handle GroupToApplicationComponentAndAccessLevelMappingEventBufferItem
                    if (nextPropertyName == ApplicationComponentPropertyName)
                    {
                        String applicationComponentAsString = ReadStringPropertyValue(ref reader);
                        TComponent applicationComponent = applicationComponentStringifier.FromString(applicationComponentAsString);
                        ReadPropertyName(ref reader, AccessLevelPropertyName);
                        String accessLevelAsString = ReadStringPropertyValue(ref reader);
                        TAccess accessLevel = accessLevelStringifier.FromString(accessLevelAsString);
                        reader.Read();
                        ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                        return new GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>(eventId, eventAction, group, applicationComponent, accessLevel, occurredTime, hashcode);
                    }
                    else
                    {
                        throw new DeserializationException($"Encountered unhandled JSON property '{nextPropertyName}' when attempting to deserialize {typeof(GroupEventBufferItem<TGroup>).Name} subclass.");
                    }
                }
                else
                {
                    ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                    return new GroupEventBufferItem<TGroup>(eventId, eventAction, group, occurredTime, hashcode);
                }
            }
            // Handle GroupToGroupMappingEventBufferItem 
            else if (nextPropertyName == FromGroupPropertyName)
            {
                String fromGroupAsString = ReadStringPropertyValue(ref reader);
                TGroup fromGroup = groupStringifier.FromString(fromGroupAsString);
                ReadPropertyName(ref reader, ToGroupPropertyName);
                String toGroupAsString = ReadStringPropertyValue(ref reader);
                TGroup toGroup = groupStringifier.FromString(toGroupAsString);
                reader.Read();
                ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.EndObject);

                return new GroupToGroupMappingEventBufferItem<TGroup>(eventId, eventAction, fromGroup, toGroup, occurredTime, hashcode);
            }
            else
            {
                throw new DeserializationException($"Encountered unhandled property '{nextPropertyName}' while attempting to deserialize {typeof(TemporalEventBufferItemBase).Name} subclass.");
            }
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TemporalEventBufferItemBase value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            // Write the properties of TemporalEventBufferItemBase
            writer.WriteString(EventIdPropertyName, value.EventId.ToString());
            writer.WriteString(EventActionPropertyName, value.EventAction.ToString());
            writer.WriteString(OccurredTimePropertyName, value.OccurredTime.ToString(dateTimeFormatString));
            writer.WriteString(HashCodePropertyName, value.HashCode.ToString());

            if (typeof(EntityTypeEventBufferItem).IsAssignableFrom(value.GetType()) == true)
            {
                writer.WriteString(EntityTypePropertyName, ((EntityTypeEventBufferItem)value).EntityType);
                if (typeof(EntityEventBufferItem).IsAssignableFrom(value.GetType()) == true)
                {
                    writer.WriteString(EntityPropertyName, ((EntityEventBufferItem)value).Entity);
                    if (typeof(UserToEntityMappingEventBufferItem<TUser>).IsAssignableFrom(value.GetType()) == true)
                    {
                        writer.WriteString(UserPropertyName, userStringifier.ToString(((UserToEntityMappingEventBufferItem<TUser>)value).User));
                    }
                    else if (typeof(GroupToEntityMappingEventBufferItem<TGroup>).IsAssignableFrom(value.GetType()) == true)
                    {
                        writer.WriteString(GroupPropertyName, groupStringifier.ToString(((GroupToEntityMappingEventBufferItem<TGroup>)value).Group));
                    }
                }
            }
            else if (value is UserEventBufferItem<TUser>)
            {
                var user = ((UserEventBufferItem<TUser>)value).User;
                writer.WriteString(UserPropertyName, userStringifier.ToString(user));
            }
            else if (value is UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>)
            {
                var userToApplicationComponentAndAccessLevelMappingEvent = (UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>)value;
                TUser user = userToApplicationComponentAndAccessLevelMappingEvent.User;
                TComponent applicationComponent = userToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent;
                TAccess accessLevel = userToApplicationComponentAndAccessLevelMappingEvent.AccessLevel;
                writer.WriteString(UserPropertyName, userStringifier.ToString(user));
                writer.WriteString(ApplicationComponentPropertyName, applicationComponentStringifier.ToString(applicationComponent));
                writer.WriteString(AccessLevelPropertyName, accessLevelStringifier.ToString(accessLevel));
            }
            else if (value is UserToGroupMappingEventBufferItem<TUser, TGroup>)
            {
                var userToGroupMappingEvent = (UserToGroupMappingEventBufferItem<TUser, TGroup>)value;
                TUser user = userToGroupMappingEvent.User;
                TGroup group = userToGroupMappingEvent.Group;
                writer.WriteString(UserPropertyName, userStringifier.ToString(user));
                writer.WriteString(GroupPropertyName, groupStringifier.ToString(group));
            }
            else if (value is GroupEventBufferItem<TGroup>)
            {
                var group = ((GroupEventBufferItem<TGroup>)value).Group;
                writer.WriteString(GroupPropertyName, groupStringifier.ToString(group));
            }
            else if (value is GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>)
            {
                var groupToApplicationComponentAndAccessLevelMappingEvent = (GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>)value;
                TGroup group = groupToApplicationComponentAndAccessLevelMappingEvent.Group;
                TComponent applicationComponent = groupToApplicationComponentAndAccessLevelMappingEvent.ApplicationComponent;
                TAccess accessLevel = groupToApplicationComponentAndAccessLevelMappingEvent.AccessLevel;
                writer.WriteString(GroupPropertyName, groupStringifier.ToString(group));
                writer.WriteString(ApplicationComponentPropertyName, applicationComponentStringifier.ToString(applicationComponent));
                writer.WriteString(AccessLevelPropertyName, accessLevelStringifier.ToString(accessLevel));
            }
            else if (value is GroupToGroupMappingEventBufferItem<TGroup>)
            {
                var groupToEntityMappingEventBufferItem = (GroupToGroupMappingEventBufferItem<TGroup>)value;
                TGroup fromGroup = groupToEntityMappingEventBufferItem.FromGroup;
                TGroup toGroup = groupToEntityMappingEventBufferItem.ToGroup;
                writer.WriteString(FromGroupPropertyName, groupStringifier.ToString(fromGroup));
                writer.WriteString(ToGroupPropertyName, groupStringifier.ToString(toGroup));
            }
            else
            {
                throw new Exception($"Encountered unhandled {typeof(TemporalEventBufferItemBase).Name} subclass '{value.GetType().Name}'.");
            }

            writer.WriteEndObject();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Reads properties of the <see cref="TemporalEventBufferItemBase"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A tuple containing the 'EventId', 'EventAction', and 'OccurredTime' properties.</returns>
        /// <exception cref="DeserializationException">If an error occurred during reading/deserialziation.</exception>
        protected Tuple<Guid, EventAction, DateTime, Int32> ReadTemporalEventBufferItemBaseProperties(ref Utf8JsonReader reader)
        {
            ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.StartObject);
            ReadPropertyName(ref reader, EventIdPropertyName);
            String eventIdAsString = ReadStringPropertyValue(ref reader);
            Boolean result = Guid.TryParse(eventIdAsString, out Guid eventId);
            if (result == false)
                throw new DeserializationException($"Failed to convert '{EventIdPropertyName}' property with value '{eventIdAsString}' to a {typeof(Guid).Name}.");
            ReadPropertyName(ref reader, EventActionPropertyName);
            String eventActionAsString = ReadStringPropertyValue(ref reader);
            result = Enum.TryParse<EventAction>(eventActionAsString, out EventAction eventAction);
            if (result == false)
                throw new DeserializationException($"Failed to convert '{EventActionPropertyName}' property with value '{eventActionAsString}' to a {typeof(EventAction).Name}.");
            ReadPropertyName(ref reader, OccurredTimePropertyName);
            String occurredTimeAsString = ReadStringPropertyValue(ref reader);
            DateTime occurredTime;
            try
            {
                occurredTime = DateTime.ParseExact(occurredTimeAsString, dateTimeFormatString, DateTimeFormatInfo.InvariantInfo);
                occurredTime = DateTime.SpecifyKind(occurredTime, DateTimeKind.Utc);
            }
            catch (FormatException fe)
            {
                throw new DeserializationException($"Failed to convert '{OccurredTimePropertyName}' property to a {typeof(DateTime).Name}.", fe);
            }

            ReadPropertyName(ref reader, HashCodePropertyName);
            String hashCodeAsString = ReadStringPropertyValue(ref reader);
            Boolean intResult = Int32.TryParse(hashCodeAsString, out Int32 hashCode);
            if (intResult == false)
                throw new DeserializationException($"Failed to convert '{HashCodePropertyName}' property with value '{hashCodeAsString}' to a {typeof(Int32).Name}.");

            return new Tuple<Guid, EventAction, DateTime, Int32>(eventId, eventAction, occurredTime, hashCode);
        }

        /// <summary>
        /// Attempts to read a property with the specified name from a JSON reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="expectedPropertyName">The expected name of the property to read.</param>
        /// <exception cref="DeserializationException">If the next token was not a property name, or the property name did not match that expected.</exception>
        protected void ReadPropertyName(ref Utf8JsonReader reader, String expectedPropertyName)
        {
            reader.Read();
            ThrowExceptionIfCurrentTokenIsNotOfType(ref reader, JsonTokenType.PropertyName);
            String actualPropertyName = reader.GetString();
            if (actualPropertyName != expectedPropertyName)
                throw new DeserializationException($"Encountered JSON property '{actualPropertyName}' when expecting to read property '{expectedPropertyName}'.");
        }

        /// <summary>
        /// Attempts to read a properties' string value from a JSON reader.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>The value of the string property.</returns>
        /// <exception cref="DeserializationException">If the next token was not a string value.</exception>
        protected String ReadStringPropertyValue(ref Utf8JsonReader reader)
        {
            reader.Read();
            if (reader.TokenType != JsonTokenType.String)
                throw new DeserializationException($"Encountered '{reader.TokenType.ToString()}' JSON token when expecting to read '{JsonTokenType.String.ToString()}'.");

            return reader.GetString();
        }

        protected void ThrowExceptionIfCurrentTokenIsNotOfType(ref Utf8JsonReader reader, JsonTokenType expectedTokenType)
        {
            if (reader.TokenType != expectedTokenType)
                throw new DeserializationException($"Expected to read JSON token '{expectedTokenType.ToString()}' but encountered '{reader.TokenType.ToString()}' when attempting to deserialize {typeof(TemporalEventBufferItemBase).Name} subclass.");
        }

        #endregion
    }
}
