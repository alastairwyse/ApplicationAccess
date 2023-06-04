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
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Serialization
{
    /// <summary>
    /// Serializes and deserializes an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> to and from a JSON document.
    /// </summary>
    public class AccessManagerJsonSerializer : IAccessManagerSerializer<JObject>
    {
        #pragma warning disable 1591

        protected const String userToGroupMapPropertyName = "userToGroupMap";
        protected const String userToComponentMapPropertyName = "userToComponentMap";
        protected const String groupToComponentMapPropertyName = "groupToComponentMap";
        protected const String entityTypesPropertyName = "entityTypes";
        protected const String userToEntityMapPropertyName = "userToEntityMap";
        protected const String groupToEntityMapPropertyName = "groupToEntityMap";
        protected const String applicationComponentPropertyName = "applicationComponent";
        protected const String accessLevelPropertyName = "accessLevel";
        protected const String userPropertyName = "user";
        protected const String groupPropertyName = "group";
        protected const String componentsPropertyName = "components";
        protected const String entityTypePropertyName = "entityType";
        protected const String entityPropertyName = "entity";
        protected const String entitiesPropertyName = "entities";

        #pragma warning restore 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Serialization.AccessManagerJsonSerializer class.
        /// </summary>
        public AccessManagerJsonSerializer()
        {
        }

        /// <summary>
        /// Serializes the specified access manager to a JSON document.
        /// </summary>
        /// <typeparam name="TUser">The type of users stored in the access manager.</typeparam>
        /// <typeparam name="TGroup">The type of groups stored in the access manager.</typeparam>
        /// <typeparam name="TComponent">The type of application components stored in the access manager.</typeparam>
        /// <typeparam name="TAccess">The type of access levels stored in the access manager.</typeparam>
        /// <param name="accessManager">The access manager to serialize.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <returns>A JSON document representing the access manager.</returns>
        public JObject Serialize<TUser, TGroup, TComponent, TAccess>
        (
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager, 
            IUniqueStringifier<TUser> userStringifier, 
            IUniqueStringifier<TGroup> groupStringifier, 
            IUniqueStringifier<TComponent> applicationComponentStringifier, 
            IUniqueStringifier<TAccess> accessLevelStringifier
        )
        {
            // TODO: Making protected methods for handling each of the top level JSON properties would make code cleaner and tests simpler

            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var returnDocument = new JObject();

            // Serialize user to group map
            JObject userToGroupMap = directedGraphSerializer.Serialize<TUser, TGroup>(ExtractUserToGroupMap<TUser, TGroup, TComponent, TAccess>(accessManager), userStringifier, groupStringifier);
            returnDocument.Add(userToGroupMapPropertyName, userToGroupMap);

            // Serialize user to application component and access level map
            var userToComponentMapJson = new JArray();
            foreach (TUser currentUser in accessManager.Users)
            {
                var applicationComponentsAndAccessLevelsJson = new JArray();
                foreach (Tuple<TComponent, TAccess> currentComponentAndAccess in accessManager.GetUserToApplicationComponentAndAccessLevelMappings(currentUser))
                {
                    var currentComponentAndAccessJson = new JObject();
                    currentComponentAndAccessJson.Add(applicationComponentPropertyName, applicationComponentStringifier.ToString(currentComponentAndAccess.Item1));
                    currentComponentAndAccessJson.Add(accessLevelPropertyName, accessLevelStringifier.ToString(currentComponentAndAccess.Item2));
                    applicationComponentsAndAccessLevelsJson.Add(currentComponentAndAccessJson);
                }
                if (applicationComponentsAndAccessLevelsJson.Count > 0)
                {
                    var currentApplicationComponentsAndAccessLevelsJson = new JObject();
                    currentApplicationComponentsAndAccessLevelsJson.Add(userPropertyName, userStringifier.ToString(currentUser));
                    currentApplicationComponentsAndAccessLevelsJson.Add(componentsPropertyName, applicationComponentsAndAccessLevelsJson);
                    userToComponentMapJson.Add(currentApplicationComponentsAndAccessLevelsJson);
                }
            }
            returnDocument.Add(userToComponentMapPropertyName, userToComponentMapJson);

            // Serialize group to application component and access level map
            var groupToComponentMapJson = new JArray();
            foreach (TGroup currentGroup in accessManager.Groups)
            {
                var applicationComponentsAndAccessLevelsJson = new JArray();
                foreach (Tuple<TComponent, TAccess> currentComponentAndAccess in accessManager.GetGroupToApplicationComponentAndAccessLevelMappings(currentGroup))
                {
                    var currentComponentAndAccessJson = new JObject();
                    currentComponentAndAccessJson.Add(applicationComponentPropertyName, applicationComponentStringifier.ToString(currentComponentAndAccess.Item1));
                    currentComponentAndAccessJson.Add(accessLevelPropertyName, accessLevelStringifier.ToString(currentComponentAndAccess.Item2));
                    applicationComponentsAndAccessLevelsJson.Add(currentComponentAndAccessJson);
                }
                if (applicationComponentsAndAccessLevelsJson.Count > 0)
                {
                    var currentApplicationComponentsAndAccessLevelsJson = new JObject();
                    currentApplicationComponentsAndAccessLevelsJson.Add(groupPropertyName, groupStringifier.ToString(currentGroup));
                    currentApplicationComponentsAndAccessLevelsJson.Add(componentsPropertyName, applicationComponentsAndAccessLevelsJson);
                    groupToComponentMapJson.Add(currentApplicationComponentsAndAccessLevelsJson);
                }
            }
            returnDocument.Add(groupToComponentMapPropertyName, groupToComponentMapJson);

            // Serialize entity types and entities
            var entityTypesJson = new JArray();
            foreach (String currentEntityType in accessManager.EntityTypes)
            {
                var entitiesJson = new JArray();
                foreach (String currentEntity in accessManager.GetEntities(currentEntityType))
                {
                    entitiesJson.Add(currentEntity);
                }
                var currentEntityTypeJson = new JObject();
                currentEntityTypeJson.Add(entityTypePropertyName, currentEntityType);
                currentEntityTypeJson.Add(entitiesPropertyName, entitiesJson);
                entityTypesJson.Add(currentEntityTypeJson);
            }
            returnDocument.Add(entityTypesPropertyName, entityTypesJson);

            // Serialize user to entity map
            var userToEntityMapJson = new JArray();
            foreach (TUser currentUser in accessManager.Users)
            {
                var currentUserMappings = new Dictionary<String, HashSet<String>>();
                foreach (Tuple<String, String> currentEntityTypeEntityPair in accessManager.GetUserToEntityMappings(currentUser))
                {
                    if (currentUserMappings.ContainsKey(currentEntityTypeEntityPair.Item1) == false)
                    {
                        currentUserMappings.Add(currentEntityTypeEntityPair.Item1, new HashSet<String>());
                    }
                    currentUserMappings[currentEntityTypeEntityPair.Item1].Add(currentEntityTypeEntityPair.Item2);
                }
                var currentUserMappingsJson = new JObject();
                if (currentUserMappings.Count > 0)
                {
                    currentUserMappingsJson.Add(userPropertyName, userStringifier.ToString(currentUser));
                    var currentEntityTypesJson = new JArray();
                    foreach (KeyValuePair<String, HashSet<String>> currentKvp in currentUserMappings)
                    {
                        var currentEntityTypeJson = new JObject();
                        currentEntityTypeJson.Add(entityTypePropertyName, currentKvp.Key);
                        var currentEntitiesJson = new JArray();
                        foreach (String currentEntity in currentKvp.Value)
                        {
                            currentEntitiesJson.Add(currentEntity);
                        }
                        currentEntityTypeJson.Add(entitiesPropertyName, currentEntitiesJson);
                        currentEntityTypesJson.Add(currentEntityTypeJson);
                    }
                    currentUserMappingsJson.Add(entityTypesPropertyName, currentEntityTypesJson);
                    userToEntityMapJson.Add(currentUserMappingsJson);
                }
            }
            returnDocument.Add(userToEntityMapPropertyName, userToEntityMapJson);

            // Serialize group to entity map
            var groupToEntityMapJson = new JArray();
            foreach (TGroup currentGroup in accessManager.Groups)
            {
                var currentGroupMappings = new Dictionary<String, HashSet<String>>();
                foreach (Tuple<String, String> currentEntityTypeEntityPair in accessManager.GetGroupToEntityMappings(currentGroup))
                {
                    if (currentGroupMappings.ContainsKey(currentEntityTypeEntityPair.Item1) == false)
                    {
                        currentGroupMappings.Add(currentEntityTypeEntityPair.Item1, new HashSet<String>());
                    }
                    currentGroupMappings[currentEntityTypeEntityPair.Item1].Add(currentEntityTypeEntityPair.Item2);
                }
                var currentGroupMappingsJson = new JObject();
                if (currentGroupMappings.Count > 0)
                {
                    currentGroupMappingsJson.Add(groupPropertyName, groupStringifier.ToString(currentGroup));
                    var currentEntityTypesJson = new JArray();
                    foreach (KeyValuePair<String, HashSet<String>> currentKvp in currentGroupMappings)
                    {
                        var currentEntityTypeJson = new JObject();
                        currentEntityTypeJson.Add(entityTypePropertyName, currentKvp.Key);
                        var currentEntitiesJson = new JArray();
                        foreach (String currentEntity in currentKvp.Value)
                        {
                            currentEntitiesJson.Add(currentEntity);
                        }
                        currentEntityTypeJson.Add(entitiesPropertyName, currentEntitiesJson);
                        currentEntityTypesJson.Add(currentEntityTypeJson);
                    }
                    currentGroupMappingsJson.Add(entityTypesPropertyName, currentEntityTypesJson);
                    groupToEntityMapJson.Add(currentGroupMappingsJson);
                }
            }
            returnDocument.Add(groupToEntityMapPropertyName, groupToEntityMapJson);

            return returnDocument;
        }

        /// <summary>
        /// Deserializes an access manager from the specified JSON document.
        /// </summary>
        /// <typeparam name="TUser">The type of users stored in the access manager.</typeparam>
        /// <typeparam name="TGroup">The type of groups stored in the access manager.</typeparam>
        /// <typeparam name="TComponent">The type of application components stored in the access manager.</typeparam>
        /// <typeparam name="TAccess">The type of access levels stored in the access manager.</typeparam>
        /// <param name="jsonDocument">The JSON document to deserialize the access manager from.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="accessManagerToDeserializeTo">The AccessManager instance to deserialize to.</param>
        /// <remarks>
        /// <para>Any existing items and mappings stored in parameter 'accessManagerToDeserializeTo' will be cleared.</para>
        /// <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow deserializing into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        public void Deserialize<TUser, TGroup, TComponent, TAccess>
        (
            JObject jsonDocument,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToDeserializeTo
        )
        {
            // Check that all top level properties exist and are of the correct type
            if (jsonDocument.ContainsKey(userToGroupMapPropertyName) == false)
                throw new ArgumentException($"JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{userToGroupMapPropertyName}' property.", nameof(jsonDocument));
            if (!(jsonDocument[userToGroupMapPropertyName] is JObject))
                throw new ArgumentException($"Property '{userToGroupMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' is not of type '{typeof(JObject)}'.", nameof(jsonDocument));
            foreach (String currentPropertyName in new String[] { userToComponentMapPropertyName, groupToComponentMapPropertyName, entityTypesPropertyName, userToEntityMapPropertyName, groupToEntityMapPropertyName })
            {
                if (jsonDocument.ContainsKey(currentPropertyName) == false)
                    throw new ArgumentException($"JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                if (!(jsonDocument[currentPropertyName] is JArray))
                    throw new ArgumentException($"Property '{currentPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' is not of type '{typeof(JArray)}'.", nameof(jsonDocument));
            }

            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            accessManagerToDeserializeTo.Clear();

            // Deserialize the user to group map
            try
            {
                var userToGroupMap = new DirectedGraph<TUser, TGroup>(false);
                directedGraphSerializer.Deserialize<TUser, TGroup>(((JObject)jsonDocument[userToGroupMapPropertyName]), userStringifier, groupStringifier, userToGroupMap);
                foreach (TUser currentUser in userToGroupMap.LeafVertices)
                {
                    accessManagerToDeserializeTo.AddUser(currentUser);
                }
                foreach (TGroup currentGroup in userToGroupMap.NonLeafVertices)
                {
                    accessManagerToDeserializeTo.AddGroup(currentGroup);
                }
                foreach (TUser currentUser in userToGroupMap.LeafVertices)
                {
                    foreach (TGroup currentGroup in userToGroupMap.GetLeafEdges(currentUser))
                    {
                        accessManagerToDeserializeTo.AddUserToGroupMapping(currentUser, currentGroup);
                    }
                }
                foreach (TGroup currentFromGroup in userToGroupMap.NonLeafVertices)
                {
                    foreach (TGroup currentToGroup in userToGroupMap.GetNonLeafEdges(currentFromGroup))
                    {
                        accessManagerToDeserializeTo.AddGroupToGroupMapping(currentFromGroup, currentToGroup);
                    }
                }
            }
            catch (Exception e)
            {
                throw new DeserializationException($"Failed to deserialize user to group map.", e);
            }

            // Deserialize user to component map
            foreach (JObject currentUserMappingsJson in (JArray)jsonDocument[userToComponentMapPropertyName])
            {
                foreach (String currentPropertyName in new String[] { userPropertyName, componentsPropertyName })
                {
                    if (currentUserMappingsJson.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{userToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentUserMappingsJson[userPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{userToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{userPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentUserMappingsJson[componentsPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{userToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{componentsPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                if (((JArray)currentUserMappingsJson[componentsPropertyName]).Count > 0)
                {
                    foreach (JToken currentComponentAndAccessLevelJson in currentUserMappingsJson[componentsPropertyName])
                    {
                        if (!(currentComponentAndAccessLevelJson is JObject))
                            throw new ArgumentException($"Property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an element which is not of type '{typeof(JObject)}'.", nameof(jsonDocument));
                        var currentComponentAndAccessLevelJObject = (JObject)currentComponentAndAccessLevelJson;
                        foreach (String currentPropertyName in new String[] { applicationComponentPropertyName, accessLevelPropertyName })
                        {
                            if (currentComponentAndAccessLevelJObject.ContainsKey(currentPropertyName) == false)
                                throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                        }
                        if (!(currentComponentAndAccessLevelJObject[applicationComponentPropertyName] is JValue))
                            throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{applicationComponentPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                        if (!(currentComponentAndAccessLevelJObject[accessLevelPropertyName] is JValue))
                            throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{accessLevelPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));

                        // TODO: Could have more granular exception handling here... separate exception handlers for de-stringifying and for adding to manager
                        try
                        {
                            TUser user = userStringifier.FromString(currentUserMappingsJson[userPropertyName].ToString());
                            TComponent applicationComponent = applicationComponentStringifier.FromString(currentComponentAndAccessLevelJObject[applicationComponentPropertyName].ToString());
                            TAccess accessLevel = accessLevelStringifier.FromString(currentComponentAndAccessLevelJObject[accessLevelPropertyName].ToString());
                            accessManagerToDeserializeTo.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
                        }
                        catch (Exception e)
                        {
                            throw new DeserializationException($"Failed to deserialize mapping for user '{currentUserMappingsJson[userPropertyName]}', application component '{currentComponentAndAccessLevelJObject[applicationComponentPropertyName]}' and access level '{currentComponentAndAccessLevelJObject[accessLevelPropertyName]}'.", e);
                        }
                    }
                }
            }

            // Deserialize group to component map
            foreach (JObject currentGroupMappingsJson in (JArray)jsonDocument[groupToComponentMapPropertyName])
            {
                foreach (String currentPropertyName in new String[] { groupPropertyName, componentsPropertyName })
                {
                    if (currentGroupMappingsJson.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{groupToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentGroupMappingsJson[groupPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{groupToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{groupPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentGroupMappingsJson[componentsPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{groupToComponentMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{componentsPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                if (((JArray)currentGroupMappingsJson[componentsPropertyName]).Count > 0)
                {
                    foreach (JToken currentComponentAndAccessLevelJson in currentGroupMappingsJson[componentsPropertyName])
                    {
                        if (!(currentComponentAndAccessLevelJson is JObject))
                            throw new ArgumentException($"Property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an element which is not of type '{typeof(JObject)}'.", nameof(jsonDocument));
                        var currentComponentAndAccessLevelJObject = (JObject)currentComponentAndAccessLevelJson;
                        foreach (String currentPropertyName in new String[] { applicationComponentPropertyName, accessLevelPropertyName })
                        {
                            if (currentComponentAndAccessLevelJObject.ContainsKey(currentPropertyName) == false)
                                throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                        }
                        if (!(currentComponentAndAccessLevelJObject[applicationComponentPropertyName] is JValue))
                            throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{applicationComponentPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                        if (!(currentComponentAndAccessLevelJObject[accessLevelPropertyName] is JValue))
                            throw new ArgumentException($"Element of property '{componentsPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{accessLevelPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));

                        try
                        {
                            TGroup group = groupStringifier.FromString(currentGroupMappingsJson[groupPropertyName].ToString());
                            TComponent applicationComponent = applicationComponentStringifier.FromString(currentComponentAndAccessLevelJObject[applicationComponentPropertyName].ToString());
                            TAccess accessLevel = accessLevelStringifier.FromString(currentComponentAndAccessLevelJObject[accessLevelPropertyName].ToString());
                            accessManagerToDeserializeTo.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
                        }
                        catch (Exception e)
                        {
                            throw new DeserializationException($"Failed to deserialize mapping for group '{currentGroupMappingsJson[groupPropertyName]}', application component '{currentComponentAndAccessLevelJObject[applicationComponentPropertyName]}' and access level '{currentComponentAndAccessLevelJObject[accessLevelPropertyName]}'.", e);
                        }
                    }
                }
            }

            // Deserialize entities
            Dictionary<String, HashSet<String>> entityTypes = DeserializeEntityStructure((JArray)jsonDocument[entityTypesPropertyName], nameof(jsonDocument));
            foreach (KeyValuePair<String, HashSet<String>> currentKvp in entityTypes)
            {
                try
                {
                    accessManagerToDeserializeTo.AddEntityType(currentKvp.Key);
                }
                catch (Exception e)
                {
                    throw new DeserializationException($"Failed to deserialize entity type '{currentKvp.Key}'.", e);
                }
                foreach (String currentEntity in currentKvp.Value)
                {
                    try
                    {
                        accessManagerToDeserializeTo.AddEntity(currentKvp.Key, currentEntity);
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize entity '{currentEntity}' of type '{currentKvp.Key}'.", e);
                    }
                }
            }

            // Deserialize user to entity map
            foreach (JObject currentUserEntityMappingsJson in (JArray)jsonDocument[userToEntityMapPropertyName])
            {
                foreach (String currentPropertyName in new String[] { userPropertyName, entityTypesPropertyName })
                {
                    if (currentUserEntityMappingsJson.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{userToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentUserEntityMappingsJson[userPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{userToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{userPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentUserEntityMappingsJson[entityTypesPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{userToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{entityTypesPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                Dictionary<String, HashSet<String>> userEntityTypes = DeserializeEntityStructure((JArray)currentUserEntityMappingsJson[entityTypesPropertyName], nameof(jsonDocument));
                foreach (KeyValuePair<String, HashSet<String>> currentKvp in userEntityTypes)
                {
                    var currentUser = default(TUser);
                    try
                    {
                        currentUser = userStringifier.FromString((currentUserEntityMappingsJson[userPropertyName].ToString()));
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize user '{currentUserEntityMappingsJson[userPropertyName]}' in user to entity mappings.", e);
                    }
                    foreach (String currentEntity in currentKvp.Value)
                    {
                        try
                        {
                            accessManagerToDeserializeTo.AddUserToEntityMapping(currentUser, currentKvp.Key, currentEntity);
                        }
                        catch (Exception e)
                        {
                            throw new DeserializationException($"Failed to deserialize entity '{currentEntity}' of type '{currentKvp.Key}' in user to entity mappings.", e);
                        }
                    }
                }
            }

            // Deserialize group to entity map
            foreach (JObject currentGroupEntityMappingsJson in (JArray)jsonDocument[groupToEntityMapPropertyName])
            {
                foreach (String currentPropertyName in new String[] { groupPropertyName, entityTypesPropertyName })
                {
                    if (currentGroupEntityMappingsJson.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{groupToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' does not contain a '{currentPropertyName}' property.", nameof(jsonDocument));
                }
                if (!(currentGroupEntityMappingsJson[groupPropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{groupToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains a '{groupPropertyName}' property which is not of type '{typeof(JValue)}'.", nameof(jsonDocument));
                if (!(currentGroupEntityMappingsJson[entityTypesPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{groupToEntityMapPropertyName}' in JSON document in parameter '{nameof(jsonDocument)}' contains an '{entityTypesPropertyName}' property which is not of type '{typeof(JArray)}'.", nameof(jsonDocument));

                Dictionary<String, HashSet<String>> groupEntityTypes = DeserializeEntityStructure((JArray)currentGroupEntityMappingsJson[entityTypesPropertyName], nameof(jsonDocument));
                foreach (KeyValuePair<String, HashSet<String>> currentKvp in groupEntityTypes)
                {
                    var currentGroup = default(TGroup);
                    try
                    {
                        currentGroup = groupStringifier.FromString((currentGroupEntityMappingsJson[groupPropertyName].ToString()));
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize group '{currentGroupEntityMappingsJson[groupPropertyName]}' in group to entity mappings.", e);
                    }
                    foreach (String currentEntity in currentKvp.Value)
                    {
                        try
                        {
                            accessManagerToDeserializeTo.AddGroupToEntityMapping(currentGroup, currentKvp.Key, currentEntity);
                        }
                        catch (Exception e)
                        {
                            throw new DeserializationException($"Failed to deserialize entity '{currentEntity}' of type '{currentKvp.Key}' in group to entity mappings.", e);
                        }
                    }
                }
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Deserializes an entity type to entity mapping structure from the specified JSON array.
        /// </summary>
        /// <param name="entityTypesValue">The JSON array to deserialize the structure from.</param>
        /// <param name="jsonDocumentParameterName">The name of the JSON document parameter the array was read from.</param>
        /// <returns>A dictionary containing the entity structure.</returns>
        /// <remarks>Several internal members of the AccessManager class use a Dictionary&lt;String, HashSet&lt;String&gt;&gt; to store entity type to entity mapping.  This method provides a common deserialization routine for them.</remarks>
        protected Dictionary<String, HashSet<String>> DeserializeEntityStructure(JArray entityTypesValue, String jsonDocumentParameterName)
        {
            // TODO: Is it possible to return a 'nested' IEnumerable instead of a Dictionary?

            var returnDictionary = new Dictionary<String, HashSet<String>>();

            foreach (JObject currentEntityTypeJson in entityTypesValue)
            {
                foreach (String currentPropertyName in new String[] { entityTypePropertyName, entitiesPropertyName })
                {
                    if (currentEntityTypeJson.ContainsKey(currentPropertyName) == false)
                        throw new ArgumentException($"Element of property '{entityTypesPropertyName}' in JSON document in parameter '{jsonDocumentParameterName}' does not contain a '{currentPropertyName}' property.", jsonDocumentParameterName);
                }
                if (!(currentEntityTypeJson[entityTypePropertyName] is JValue))
                    throw new ArgumentException($"Element of property '{entityTypesPropertyName}' in JSON document in parameter '{jsonDocumentParameterName}' contains an '{entityTypePropertyName}' property which is not of type '{typeof(JValue)}'.", jsonDocumentParameterName);
                if (!(currentEntityTypeJson[entitiesPropertyName] is JArray))
                    throw new ArgumentException($"Element of property '{entityTypesPropertyName}' in JSON document in parameter '{jsonDocumentParameterName}' contains an '{entitiesPropertyName}' property which is not of type '{typeof(JArray)}'.", jsonDocumentParameterName);

                try
                {
                    returnDictionary.Add(currentEntityTypeJson[entityTypePropertyName].ToString(), new HashSet<String>());
                }
                catch (Exception e)
                {
                    throw new DeserializationException($"Failed to deserialize entity type '{currentEntityTypeJson[entityTypePropertyName]}'.", e);
                }
                foreach (JValue currentEntity in (JArray)currentEntityTypeJson[entitiesPropertyName])
                {
                    try
                    {
                        returnDictionary[currentEntityTypeJson[entityTypePropertyName].ToString()].Add(currentEntity.ToString());
                    }
                    catch (Exception e)
                    {
                        throw new DeserializationException($"Failed to deserialize entity '{currentEntity}' of type '{currentEntityTypeJson[entityTypePropertyName]}'.", e);
                    }
                }
            }

            return returnDictionary;
        }

        /// <summary>
        /// Replicates and returns DirectedGraph representing the user to group mapping structure in the specified access manager.
        /// </summary>
        /// <typeparam name="TUser">The type of users stored in the access manager.</typeparam>
        /// <typeparam name="TGroup">The type of groups stored in the access manager.</typeparam>
        /// <typeparam name="TComponent">The type of application components stored in the access manager.</typeparam>
        /// <typeparam name="TAccess">The type of access levels stored in the access manager.</typeparam>
        /// <param name="accessManager">The access manager to extract the user to group mapping data from.</param>
        /// <returns>The user to group mapping structure in the access manager</returns>
        protected DirectedGraph<TUser, TGroup> ExtractUserToGroupMap<TUser, TGroup, TComponent, TAccess>(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager)
        {
            var returnGraph = new DirectedGraph<TUser, TGroup>(false);

            foreach (TUser currentUser in accessManager.Users)
            {
                returnGraph.AddLeafVertex(currentUser);
            }
            foreach (TGroup currentGroup in accessManager.Groups)
            {
                returnGraph.AddNonLeafVertex(currentGroup);
            }
            foreach (TUser currentUser in accessManager.Users)
            {
                foreach (TGroup currentGroup in accessManager.GetUserToGroupMappings(currentUser, false))
                {
                    returnGraph.AddLeafToNonLeafEdge(currentUser, currentGroup);
                }
            }
            foreach (TGroup currentFromGroup in accessManager.Groups)
            {
                foreach (TGroup currentToGroup in accessManager.GetGroupToGroupMappings(currentFromGroup, false))
                {
                    returnGraph.AddNonLeafToNonLeafEdge(currentFromGroup, currentToGroup);
                }
            }

            return returnGraph;
        }

        #endregion
    }
}
