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
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Serialization.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Serialization.AccessManagerJsonSerializer class.
    /// </summary>
    public class AccessManagerJsonSerializerUnitTests
    {
        private AccessManagerJsonSerializer testAccessManagerJsonSerializer;
        private AccessManager<String, String, ApplicationScreen, AccessLevel> accessManagerToDeserializeTo;

        [SetUp]
        protected void SetUp()
        {
            testAccessManagerJsonSerializer = new AccessManagerJsonSerializer();
            accessManagerToDeserializeTo = new AccessManager<String, String, ApplicationScreen, AccessLevel>();
        }

        [Test]
        public void Serialize_EmptyGraph()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testAccessManager = new AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>();
            var comparisonDocument = new JObject();
            comparisonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            comparisonDocument.Add("userToComponentMap", new JArray());
            comparisonDocument.Add("groupToComponentMap", new JArray());
            comparisonDocument.Add("entityTypes", new JArray());
            comparisonDocument.Add("userToEntityMap", new JArray());
            comparisonDocument.Add("groupToEntityMap", new JArray());

            JObject result = testAccessManagerJsonSerializer.Serialize<String, String, ApplicationScreen, AccessLevel>
            (
                testAccessManager,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>()
            );

            Assert.AreEqual(comparisonDocument.ToString(), result.ToString());
        }

        [Test]
        public void Serialize()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testAccessManager = new AccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>();
            CreateTestData(testAccessManager);
            JObject serializedDirectedGraph = directedGraphSerializer.Serialize<String, String>(testAccessManager.UserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier());
            var screenStringifier = new EnumUniqueStringifier<ApplicationScreen>();
            var accessLevelStringifier = new EnumUniqueStringifier<AccessLevel>();

            var comparisonDocument = new JObject();
            comparisonDocument.Add("userToGroupMap", serializedDirectedGraph);
            var userToComponentMapJson = new JArray();
            var groupToComponentMapJson = new JArray();
            var entityTypesJson = new JArray();
            var userToEntityMapJson = new JArray();
            var groupToEntityMapJson = new JArray();
            foreach (String currentUser in testAccessManager.Users)
            {
                var currentUserToComponentMapEntryJson = new JObject();
                var screenAndAccessLevelJson = new JArray();
                foreach (Tuple<ApplicationScreen, AccessLevel> currentScreenAndAccessLevel in testAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(currentUser))
                {
                    var currentScreenAndAccessLevelJson = new JObject();
                    currentScreenAndAccessLevelJson.Add("applicationComponent", screenStringifier.ToString(currentScreenAndAccessLevel.Item1));
                    currentScreenAndAccessLevelJson.Add("accessLevel", accessLevelStringifier.ToString(currentScreenAndAccessLevel.Item2));
                    screenAndAccessLevelJson.Add(currentScreenAndAccessLevelJson);
                }
                if (screenAndAccessLevelJson.Count > 0)
                {
                    currentUserToComponentMapEntryJson.Add("user", currentUser);
                    currentUserToComponentMapEntryJson.Add("components", screenAndAccessLevelJson);
                    userToComponentMapJson.Add(currentUserToComponentMapEntryJson);
                }
            }
            comparisonDocument.Add("userToComponentMap", userToComponentMapJson);
            foreach (String currentGroup in testAccessManager.Groups)
            {
                var currentGroupToComponentMapEntryJson = new JObject();
                var screenAndAccessLevelJson = new JArray();
                foreach (Tuple<ApplicationScreen, AccessLevel> currentScreenAndAccessLevel in testAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(currentGroup))
                {
                    var currentScreenAndAccessLevelJson = new JObject();
                    currentScreenAndAccessLevelJson.Add("applicationComponent", screenStringifier.ToString(currentScreenAndAccessLevel.Item1));
                    currentScreenAndAccessLevelJson.Add("accessLevel", accessLevelStringifier.ToString(currentScreenAndAccessLevel.Item2));
                    screenAndAccessLevelJson.Add(currentScreenAndAccessLevelJson);
                }
                if (screenAndAccessLevelJson.Count > 0)
                {
                    currentGroupToComponentMapEntryJson.Add("group", currentGroup);
                    currentGroupToComponentMapEntryJson.Add("components", screenAndAccessLevelJson);
                    groupToComponentMapJson.Add(currentGroupToComponentMapEntryJson);
                }
            }
            comparisonDocument.Add("groupToComponentMap", groupToComponentMapJson);
            foreach (String currentEntityType in testAccessManager.EntityTypes)
            {
                var currentEntityTypeJson = new JObject();
                currentEntityTypeJson.Add("entityType", currentEntityType);
                var currentEntitiesJson = new JArray();
                foreach (String currentEntity in testAccessManager.GetEntities(currentEntityType))
                {
                    currentEntitiesJson.Add(currentEntity);
                }
                currentEntityTypeJson.Add("entities", currentEntitiesJson);
                entityTypesJson.Add(currentEntityTypeJson);
            }
            comparisonDocument.Add("entityTypes", entityTypesJson);
            foreach (String currentUser in testAccessManager.Users)
            {
                if (testAccessManager.GetUserToEntityMappings(currentUser).Count() > 0)
                {
                    var currentUserToEntityJson = new JObject();
                    currentUserToEntityJson.Add("user", currentUser);
                    var entityStructure = new Dictionary<String, HashSet<String>>();
                    foreach (Tuple<String, String> currentEntityTypeAndEntity in testAccessManager.GetUserToEntityMappings(currentUser))
                    {
                        if (entityStructure.ContainsKey(currentEntityTypeAndEntity.Item1) == false)
                        {
                            entityStructure.Add(currentEntityTypeAndEntity.Item1, new HashSet<String>());
                        }
                        entityStructure[currentEntityTypeAndEntity.Item1].Add(currentEntityTypeAndEntity.Item2);
                    }
                    var currentEntityTypesJson = new JArray();
                    foreach (KeyValuePair<String, HashSet<String>> currentKvp in entityStructure)
                    {
                        var currentEntityTypeJson = new JObject();
                        currentEntityTypeJson.Add("entityType", currentKvp.Key);
                        var currentEntitiesJson = new JArray();
                        foreach (String currentEntity in currentKvp.Value)
                        {
                            currentEntitiesJson.Add(currentEntity);
                        }
                        currentEntityTypeJson.Add("entities", currentEntitiesJson);
                        currentEntityTypesJson.Add(currentEntityTypeJson);
                    }
                    currentUserToEntityJson.Add("entityTypes", currentEntityTypesJson);
                    userToEntityMapJson.Add(currentUserToEntityJson);
                }
            }
            comparisonDocument.Add("userToEntityMap", userToEntityMapJson);
            foreach (String currentGroup in testAccessManager.Groups)
            {
                if (testAccessManager.GetGroupToEntityMappings(currentGroup).Count() > 0)
                {
                    var currentGroupToEntityJson = new JObject();
                    currentGroupToEntityJson.Add("group", currentGroup);
                    var entityStructure = new Dictionary<String, HashSet<String>>();
                    foreach (Tuple<String, String> currentEntityTypeAndEntity in testAccessManager.GetGroupToEntityMappings(currentGroup))
                    {
                        if (entityStructure.ContainsKey(currentEntityTypeAndEntity.Item1) == false)
                        {
                            entityStructure.Add(currentEntityTypeAndEntity.Item1, new HashSet<String>());
                        }
                        entityStructure[currentEntityTypeAndEntity.Item1].Add(currentEntityTypeAndEntity.Item2);
                    }
                    var currentEntityTypesJson = new JArray();
                    foreach (KeyValuePair<String, HashSet<String>> currentKvp in entityStructure)
                    {
                        var currentEntityTypeJson = new JObject();
                        currentEntityTypeJson.Add("entityType", currentKvp.Key);
                        var currentEntitiesJson = new JArray();
                        foreach (String currentEntity in currentKvp.Value)
                        {
                            currentEntitiesJson.Add(currentEntity);
                        }
                        currentEntityTypeJson.Add("entities", currentEntitiesJson);
                        currentEntityTypesJson.Add(currentEntityTypeJson);
                    }
                    currentGroupToEntityJson.Add("entityTypes", currentEntityTypesJson);
                    groupToEntityMapJson.Add(currentGroupToEntityJson);
                }
            }
            comparisonDocument.Add("groupToEntityMap", groupToEntityMapJson);

            JObject result = testAccessManagerJsonSerializer.Serialize<String, String, ApplicationScreen, AccessLevel>
            (
                testAccessManager, 
                new StringUniqueStringifier(), 
                new StringUniqueStringifier(), 
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>()
            );

            Assert.AreEqual(comparisonDocument.ToString(), result.ToString());
        }

        [Test]
        public void Deserialize_UserToGroupMapPropertyDoesNotExist()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(), 
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'userToGroupMap' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'userToComponentMap' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'groupToComponentMap' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_EntityTypesPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'entityTypes' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToEntityMapPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'userToEntityMap' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToEntityMapPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'jsonDocument' does not contain a 'groupToEntityMap' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToGroupMapPropertyIsNotObject()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", new JArray());
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'userToGroupMap' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JObject)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapPropertyIsNotArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JObject());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'userToComponentMap' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapPropertyIsNotArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JObject());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'groupToComponentMap' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_EntityTypesPropertyIsNotArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JObject());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(), 
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'entityTypes' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToEntityMapPropertyIsNotArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JObject());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'userToEntityMap' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToEntityMapPropertyIsNotArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JObject());

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'groupToEntityMap' in JSON document in parameter 'jsonDocument' is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeUserToGroupMap()
        {
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", new JObject());
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize user to group map."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementUserPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("components", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'userToComponentMap' in JSON document in parameter 'jsonDocument' does not contain a 'user' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'userToComponentMap' in JSON document in parameter 'jsonDocument' does not contain a 'components' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementUserPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", new JArray()), 
                    new JProperty("components", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'userToComponentMap' in JSON document in parameter 'jsonDocument' contains a 'user' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsPropertyIsNotJArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", "SingularValue")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'userToComponentMap' in JSON document in parameter 'jsonDocument' contains a 'components' property which is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsElementIsNotJObject()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JArray());
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'components' in JSON document in parameter 'jsonDocument' contains an element which is not of type '{typeof(JObject)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsElementApplicationComponentPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (   
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' does not contain a 'applicationComponent' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsElementAccessLevelPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' does not contain a 'accessLevel' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsElementApplicationComponentPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", new JArray()),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' contains an 'applicationComponent' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToComponentMapElementComponentsElementAccessLevelPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", new JArray())
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' contains an 'accessLevel' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeUserToComponentMapElement()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testUserToGroupMap = new DirectedGraph<String, String>();
            testUserToGroupMap.AddLeafVertex("User1");
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "InvalidUser"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for user 'InvalidUser', application component 'Order' and access level 'View'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);


            testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "InvalidApplicationComponent"),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for user 'User1', application component 'InvalidApplicationComponent' and access level 'View'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);


            testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", "InvalidAccessLevel")
            ));
            ((JArray)testJsonDocument["userToComponentMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("components", componentsProperty)
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for user 'User1', application component 'Order' and access level 'InvalidAccessLevel'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementGroupPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("components", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'groupToComponentMap' in JSON document in parameter 'jsonDocument' does not contain a 'group' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'groupToComponentMap' in JSON document in parameter 'jsonDocument' does not contain a 'components' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementGroupPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", new JArray()),
                    new JProperty("components", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'groupToComponentMap' in JSON document in parameter 'jsonDocument' contains a 'group' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsPropertyIsNotJArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", "SingularValue")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'groupToComponentMap' in JSON document in parameter 'jsonDocument' contains a 'components' property which is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsElementIsNotJObject()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JArray());
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Property 'components' in JSON document in parameter 'jsonDocument' contains an element which is not of type '{typeof(JObject)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsElementApplicationComponentPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' does not contain a 'applicationComponent' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsElementAccessLevelPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' does not contain a 'accessLevel' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsElementApplicationComponentPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", new JArray()),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' contains an 'applicationComponent' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToComponentMapElementComponentsElementAccessLevelPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", new JArray())
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'components' in JSON document in parameter 'jsonDocument' contains an 'accessLevel' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeGroupToComponentMapElement()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testUserToGroupMap = new DirectedGraph<String, String>();
            testUserToGroupMap.AddNonLeafVertex("Group1");
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "InvalidGroup"),
                    new JProperty("components", componentsProperty)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for group 'InvalidGroup', application component 'Order' and access level 'View'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);


            testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "InvalidApplicationComponent"),
                new JProperty("accessLevel", "View")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for group 'Group1', application component 'InvalidApplicationComponent' and access level 'View'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);


            testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(testUserToGroupMap, new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            componentsProperty = new JArray();
            componentsProperty.Add(new JObject
            (
                new JProperty("applicationComponent", "Order"),
                new JProperty("accessLevel", "InvalidAccessLevel")
            ));
            ((JArray)testJsonDocument["groupToComponentMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("components", componentsProperty)
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize mapping for group 'Group1', application component 'Order' and access level 'InvalidAccessLevel'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);
        }

        [Test]
        public void Deserialize_EntityTypesElementEntityTypePropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'entityTypes' in JSON document in parameter 'jsonDocument' does not contain a 'entityType' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_EntityTypesElementEntitiesPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "ClientAccount")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'entityTypes' in JSON document in parameter 'jsonDocument' does not contain a 'entities' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_EntityTypesElementEntityTypePropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", new JArray()),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'entityTypes' in JSON document in parameter 'jsonDocument' contains an 'entityType' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_EntityTypesElementEntitiesPropertyIsNotJArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JObject())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'entityTypes' in JSON document in parameter 'jsonDocument' contains an 'entities' property which is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeEntityTypesElementEntityTypeProperty()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "  "),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize entity type '  '."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);


            testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );

            e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize entity type 'ClientAccount'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);
        }

        [Test]
        public void Deserialize_FailureToDeserializeEntityTypesElementEntitiesProperty()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["entityTypes"]).Add(new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "  ", "CompanyC"))
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize entity '  ' with type 'ClientAccount'."));
            Assert.IsInstanceOf<ArgumentException>(e.InnerException);
        }

        [Test]
        public void Deserialize_UserToEntityMapElementUserPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("entityTypes", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'userToEntityMap' in JSON document in parameter 'jsonDocument' does not contain a 'user' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToEntityMapElementEntityTypesPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("user", "User1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'userToEntityMap' in JSON document in parameter 'jsonDocument' does not contain a 'entityTypes' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToEntityMapElementUserPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("user", new JArray()),
                    new JProperty("entityTypes", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'userToEntityMap' in JSON document in parameter 'jsonDocument' contains a 'user' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_UserToEntityMapElementEntityTypesPropertyIsNotJArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("entityTypes", "SingularValue")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'userToEntityMap' in JSON document in parameter 'jsonDocument' contains an 'entityTypes' property which is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeUserToEntityMapElementUserProperty()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var entityTypeJson = new JArray();
            entityTypeJson.Add(
                new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("entityTypes", entityTypeJson)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new ExceptionThrowingStringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize user 'User1' in user to entity mappings."));
            Assert.IsAssignableFrom<NotImplementedException>(e.InnerException);
        }

        [Test]
        public void Deserialize_FailureToDeserializeUserToEntityMapElementEntityTypesPropertyEntitiesElement()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var entityTypeJson = new JArray();
            entityTypeJson.Add(
                new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "  ", "CompanyC"))
                )
            );
            ((JArray)testJsonDocument["userToEntityMap"]).Add(new JObject
                (
                    new JProperty("user", "User1"),
                    new JProperty("entityTypes", entityTypeJson)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize entity 'CompanyA' with type 'ClientAccount' in user to entity mappings."));
            Assert.IsAssignableFrom<UserNotFoundException<String>>(e.InnerException);
        }

        [Test]
        public void Deserialize_EmptyGraph()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());

            testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
            (
                testJsonDocument,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>(),
                accessManagerToDeserializeTo
            );
        }


        [Test]
        public void Deserialize_AccessManagerToDeserializeToParameterCleared()
        {
            JObject testJsonDocument = CreateTestDataJson();
            accessManagerToDeserializeTo.AddUser("UserA");
            accessManagerToDeserializeTo.AddGroup("GroupA");
            accessManagerToDeserializeTo.AddGroup("GroupB");
            accessManagerToDeserializeTo.AddUserToGroupMapping("UserA", "GroupA");
            accessManagerToDeserializeTo.AddGroupToGroupMapping("GroupA", "GroupB");
            accessManagerToDeserializeTo.AddUserToApplicationComponentAndAccessLevelMapping("UserA", ApplicationScreen.Order, AccessLevel.View);
            accessManagerToDeserializeTo.AddGroupToApplicationComponentAndAccessLevelMapping("GroupB", ApplicationScreen.ManageProducts, AccessLevel.View);
            accessManagerToDeserializeTo.AddEntityType("BankAccount");
            accessManagerToDeserializeTo.AddEntity("BankAccount", "Savings");
            accessManagerToDeserializeTo.AddUserToEntityMapping("UserA", "BankAccount", "Savings");
            accessManagerToDeserializeTo.AddGroupToEntityMapping("GroupB", "BankAccount", "Savings");

            testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
            (
                testJsonDocument,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>(),
                accessManagerToDeserializeTo
            );

            AssertTestData(accessManagerToDeserializeTo);
        }


        [Test]
        public void Deserialize()
        {
            JObject testJsonDocument = CreateTestDataJson();

            testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
            (
                testJsonDocument,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>(),
                accessManagerToDeserializeTo
            );

            AssertTestData(accessManagerToDeserializeTo);
        }

        [Test]
        public void Deserialize_GroupToEntityMapElementGroupPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("entityTypes", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'groupToEntityMap' in JSON document in parameter 'jsonDocument' does not contain a 'group' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToEntityMapElementEntityTypesPropertyDoesNotExist()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith("Element of property 'groupToEntityMap' in JSON document in parameter 'jsonDocument' does not contain a 'entityTypes' property."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToEntityMapElementGroupPropertyIsNotJValue()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("group", new JArray()),
                    new JProperty("entityTypes", new JArray())
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'groupToEntityMap' in JSON document in parameter 'jsonDocument' contains a 'group' property which is not of type '{typeof(JValue)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_GroupToEntityMapElementEntityTypesPropertyIsNotJArray()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("entityTypes", "SingularValue")
                )
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Element of property 'groupToEntityMap' in JSON document in parameter 'jsonDocument' contains an 'entityTypes' property which is not of type '{typeof(JArray)}'."));
            Assert.AreEqual("jsonDocument", e.ParamName);
        }

        [Test]
        public void Deserialize_FailureToDeserializeGroupToEntityMapElementGroupProperty()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var entityTypeJson = new JArray();
            entityTypeJson.Add(
                new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "CompanyB"))
                )
            );
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("entityTypes", entityTypeJson)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new ExceptionThrowingStringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize group 'Group1' in group to entity mappings."));
            Assert.IsAssignableFrom<NotImplementedException>(e.InnerException);
        }

        [Test]
        public void Deserialize_FailureToDeserializeGroupToEntityMapElementEntityTypesPropertyEntitiesElement()
        {
            var directedGraphSerializer = new DirectedGraphJsonSerializer();
            var testJsonDocument = new JObject();
            testJsonDocument.Add("userToGroupMap", directedGraphSerializer.Serialize<String, String>(new DirectedGraph<String, String>(), new StringUniqueStringifier(), new StringUniqueStringifier()));
            testJsonDocument.Add("userToComponentMap", new JArray());
            testJsonDocument.Add("groupToComponentMap", new JArray());
            testJsonDocument.Add("entityTypes", new JArray());
            testJsonDocument.Add("userToEntityMap", new JArray());
            testJsonDocument.Add("groupToEntityMap", new JArray());
            var entityTypeJson = new JArray();
            entityTypeJson.Add(
                new JObject
                (
                    new JProperty("entityType", "ClientAccount"),
                    new JProperty("entities", new JArray("CompanyA", "  ", "CompanyC"))
                )
            );
            ((JArray)testJsonDocument["groupToEntityMap"]).Add(new JObject
                (
                    new JProperty("group", "Group1"),
                    new JProperty("entityTypes", entityTypeJson)
                )
            );

            var e = Assert.Throws<DeserializationException>(delegate
            {
                testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
                (
                    testJsonDocument,
                    new StringUniqueStringifier(),
                    new StringUniqueStringifier(),
                    new EnumUniqueStringifier<ApplicationScreen>(),
                    new EnumUniqueStringifier<AccessLevel>(),
                    accessManagerToDeserializeTo
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to deserialize entity 'CompanyA' with type 'ClientAccount' in group to entity mappings."));
            Assert.IsAssignableFrom<GroupNotFoundException<String>>(e.InnerException);
        }

        [Test]
        public void SerializeDeserialize()
        {
            var testAccessManager = new AccessManager<String, String, ApplicationScreen, AccessLevel>();
            CreateTestData(testAccessManager);

            JObject serializedAccessManager = testAccessManagerJsonSerializer.Serialize<String, String, ApplicationScreen, AccessLevel>
            (
                testAccessManager,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>()
            );
            testAccessManagerJsonSerializer.Deserialize<String, String, ApplicationScreen, AccessLevel>
            (
                serializedAccessManager,
                new StringUniqueStringifier(),
                new StringUniqueStringifier(),
                new EnumUniqueStringifier<ApplicationScreen>(),
                new EnumUniqueStringifier<AccessLevel>(),
                accessManagerToDeserializeTo
            );

            AssertTestData(accessManagerToDeserializeTo);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Adds test data to the specified access manager.
        /// </summary>
        /// <param name="inputGraph">The graph to create the sample structure in.</param>
        protected void CreateTestData(AccessManager<String, String, ApplicationScreen, AccessLevel> inputAccessManager)
        {
            inputAccessManager.AddUser("User1");
            inputAccessManager.AddUser("User2");
            inputAccessManager.AddUser("User3");
            inputAccessManager.AddUser("User4");
            inputAccessManager.AddGroup("Group1");
            inputAccessManager.AddGroup("Group2");
            inputAccessManager.AddGroup("Group3");
            inputAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("User1", ApplicationScreen.Order, AccessLevel.View);
            inputAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("User2", ApplicationScreen.Order, AccessLevel.View);
            inputAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("User2", ApplicationScreen.Order, AccessLevel.Modify);
            inputAccessManager.AddUserToApplicationComponentAndAccessLevelMapping("User2", ApplicationScreen.Settings, AccessLevel.View);
            inputAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Group1", ApplicationScreen.Summary, AccessLevel.View);
            inputAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Group2", ApplicationScreen.Order, AccessLevel.View);
            inputAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Group2", ApplicationScreen.Order, AccessLevel.Modify);
            inputAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("Group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            inputAccessManager.AddEntityType("ClientAccount");
            inputAccessManager.AddEntity("ClientAccount", "CompanyA");
            inputAccessManager.AddEntity("ClientAccount", "CompanyB");
            inputAccessManager.AddEntity("ClientAccount", "CompanyC");
            inputAccessManager.AddEntityType("BusinessUnit");
            inputAccessManager.AddEntity("BusinessUnit", "Marketing");
            inputAccessManager.AddEntity("BusinessUnit", "Sales");
            inputAccessManager.AddEntityType("ProductLine");
            inputAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyA");
            inputAccessManager.AddUserToEntityMapping("User2", "ClientAccount", "CompanyB");
            inputAccessManager.AddUserToEntityMapping("User2", "BusinessUnit", "Sales");
            inputAccessManager.AddUserToEntityMapping("User3", "BusinessUnit", "Marketing");
            inputAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyB");
            inputAccessManager.AddGroupToEntityMapping("Group2", "ClientAccount", "CompanyC");
            inputAccessManager.AddGroupToEntityMapping("Group2", "BusinessUnit", "Marketing");
            inputAccessManager.AddGroupToEntityMapping("Group3", "BusinessUnit", "Sales");
        }

        /// <summary>
        /// Creates a JSON object containing a serialized version of the test data.
        /// </summary>
        /// <returns>A JSON object containing a serialized version of the test data.</returns>
        protected JObject CreateTestDataJson()
        {

            String stringifiedAccessManager = @"
            {
                ""userToGroupMap"": {
                ""leafVertices"": [
                    ""User1"",
                    ""User2"",
                    ""User3"",
                    ""User4""
                ],
                ""leafToNonLeafEdges"": [],
                ""nonLeafVertices"": [
                    ""Group1"",
                    ""Group2"",
                    ""Group3""
                ],
                ""nonLeafToNonLeafEdges"": []
                },
                ""userToComponentMap"": [
                {
                    ""user"": ""User1"",
                    ""components"": [
                    {
                        ""applicationComponent"": ""Order"",
                        ""accessLevel"": ""View""
                    }
                    ]
                },
                {
                    ""user"": ""User2"",
                    ""components"": [
                    {
                        ""applicationComponent"": ""Order"",
                        ""accessLevel"": ""View""
                    },
                    {
                        ""applicationComponent"": ""Order"",
                        ""accessLevel"": ""Modify""
                    },
                    {
                        ""applicationComponent"": ""Settings"",
                        ""accessLevel"": ""View""
                    }
                    ]
                }
                ],
                ""groupToComponentMap"": [
                {
                    ""group"": ""Group1"",
                    ""components"": [
                    {
                        ""applicationComponent"": ""Summary"",
                        ""accessLevel"": ""View""
                    }
                    ]
                },
                {
                    ""group"": ""Group2"",
                    ""components"": [
                    {
                        ""applicationComponent"": ""Order"",
                        ""accessLevel"": ""View""
                    },
                    {
                        ""applicationComponent"": ""Order"",
                        ""accessLevel"": ""Modify""
                    },
                    {
                        ""applicationComponent"": ""ManageProducts"",
                        ""accessLevel"": ""Modify""
                    }
                    ]
                }
                ],
                ""entityTypes"": [
                {
                    ""entityType"": ""ClientAccount"",
                    ""entities"": [
                    ""CompanyA"",
                    ""CompanyB"",
                    ""CompanyC""
                    ]
                },
                {
                    ""entityType"": ""BusinessUnit"",
                    ""entities"": [
                    ""Marketing"",
                    ""Sales""
                    ]
                },
                {
                    ""entityType"": ""ProductLine"",
                    ""entities"": []
                }
                ],
                ""userToEntityMap"": [
                {
                    ""user"": ""User2"",
                    ""entityTypes"": [
                    {
                        ""entityType"": ""ClientAccount"",
                        ""entities"": [
                        ""CompanyA"",
                        ""CompanyB""
                        ]
                    },
                    {
                        ""entityType"": ""BusinessUnit"",
                        ""entities"": [
                        ""Sales""
                        ]
                    }
                    ]
                },
                {
                    ""user"": ""User3"",
                    ""entityTypes"": [
                    {
                        ""entityType"": ""BusinessUnit"",
                        ""entities"": [
                        ""Marketing""
                        ]
                    }
                    ]
                }
                ],
                ""groupToEntityMap"": [
                {
                    ""group"": ""Group2"",
                    ""entityTypes"": [
                    {
                        ""entityType"": ""ClientAccount"",
                        ""entities"": [
                        ""CompanyB"",
                        ""CompanyC""
                        ]
                    },
                    {
                        ""entityType"": ""BusinessUnit"",
                        ""entities"": [
                        ""Marketing""
                        ]
                    }
                    ]
                },
                {
                    ""group"": ""Group3"",
                    ""entityTypes"": [
                    {
                        ""entityType"": ""BusinessUnit"",
                        ""entities"": [
                        ""Sales""
                        ]
                    }
                    ]
                }
                ]
            }";
            
            return JObject.Parse(stringifiedAccessManager);
        }

        /// <summary>
        /// Asserts that the structure of the specified access manager matches the structure setup in method CreateTestData().
        /// </summary>
        /// <param name="inputAccessManager">The access manager to check.</param>
        protected void AssertTestData(AccessManager<String, String, ApplicationScreen, AccessLevel> inputAccessManager)
        {
            Assert.AreEqual(4, inputAccessManager.Users.Count());
            Assert.AreEqual(3, inputAccessManager.Groups.Count());
            Assert.AreEqual(3, inputAccessManager.EntityTypes.Count());
            Assert.AreEqual(1, inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User1").Count());
            Assert.IsTrue(inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User1").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.AreEqual(3, inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User2").Count());
            Assert.IsTrue(inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
            Assert.IsTrue(inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Settings, AccessLevel.View)));
            Assert.AreEqual(0, inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User3").Count());
            Assert.AreEqual(0, inputAccessManager.GetUserToApplicationComponentAndAccessLevelMappings("User4").Count());
            Assert.AreEqual(1, inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group1").Count());
            Assert.IsTrue(inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group1").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Summary, AccessLevel.View)));
            Assert.AreEqual(3, inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group2").Count());
            Assert.IsTrue(inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.View)));
            Assert.IsTrue(inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
            Assert.IsTrue(inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group2").Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
            Assert.AreEqual(0, inputAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings("Group3").Count());
            Assert.AreEqual(3, inputAccessManager.EntityTypes.Count());
            Assert.IsTrue(inputAccessManager.ContainsEntityType("ClientAccount"));
            Assert.IsTrue(inputAccessManager.ContainsEntityType("BusinessUnit"));
            Assert.IsTrue(inputAccessManager.ContainsEntityType("ProductLine"));
            Assert.AreEqual(3, inputAccessManager.GetEntities("ClientAccount").Count());
            Assert.IsTrue(inputAccessManager.ContainsEntity("ClientAccount", "CompanyA"));
            Assert.IsTrue(inputAccessManager.ContainsEntity("ClientAccount", "CompanyB"));
            Assert.IsTrue(inputAccessManager.ContainsEntity("ClientAccount", "CompanyC"));
            Assert.AreEqual(2, inputAccessManager.GetEntities("BusinessUnit").Count());
            Assert.IsTrue(inputAccessManager.ContainsEntity("BusinessUnit", "Marketing"));
            Assert.IsTrue(inputAccessManager.ContainsEntity("BusinessUnit", "Sales"));
            Assert.AreEqual(0, inputAccessManager.GetEntities("ProductLine").Count());
            Assert.AreEqual(3, inputAccessManager.GetUserToEntityMappings("User2").Count());
            Assert.IsTrue(inputAccessManager.GetUserToEntityMappings("User2").Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(inputAccessManager.GetUserToEntityMappings("User2").Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(inputAccessManager.GetUserToEntityMappings("User2").Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.AreEqual(1, inputAccessManager.GetUserToEntityMappings("User3").Count());
            Assert.IsTrue(inputAccessManager.GetUserToEntityMappings("User3").Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            Assert.AreEqual(0, inputAccessManager.GetUserToEntityMappings("User1").Count());
            Assert.AreEqual(0, inputAccessManager.GetUserToEntityMappings("User4").Count());
            Assert.AreEqual(3, inputAccessManager.GetGroupToEntityMappings("Group2").Count());
            Assert.IsTrue(inputAccessManager.GetGroupToEntityMappings("Group2").Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(inputAccessManager.GetGroupToEntityMappings("Group2").Contains(new Tuple<String, String>("ClientAccount", "CompanyC")));
            Assert.IsTrue(inputAccessManager.GetGroupToEntityMappings("Group2").Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
            Assert.AreEqual(1, inputAccessManager.GetGroupToEntityMappings("Group3").Count());
            Assert.IsTrue(inputAccessManager.GetGroupToEntityMappings("Group3").Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.AreEqual(0, inputAccessManager.GetGroupToEntityMappings("Group1").Count());
        }

        #endregion

        #region Nested Classes

        protected enum ApplicationScreen
        {
            Order,
            Summary,
            ManageProducts,
            Settings
        }

        protected enum AccessLevel
        {
            View,
            Create,
            Modify,
            Delete
        }

        /// <summary>
        /// Version of the AccessManager class where protected members are exposed as public.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class AccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManager<TUser, TGroup, TComponent, TAccess>
        {
            public AccessManagerWithProtectedMembers()
                : base()
            {
            }

            /// <summary>The DirectedGraph which stores the user to group mappings.</summary>
            public DirectedGraphBase<TUser, TGroup> UserToGroupMap
            {
                get { return userToGroupMap; }
            }
        }

        /// <summary>
        /// Implementation of IUniqueStringifier&lt;String&gt; where the methods throw exceptions, for testing of exception handling.
        /// </summary>
        protected class ExceptionThrowingStringUniqueStringifier : IUniqueStringifier<String>
        {
            public string FromString(string stringifiedObject)
            {
                throw new NotImplementedException();
            }

            public string ToString(string inputObject)
            {
                throw new NotImplementedException();
            }
        }

        #endregion
    }
}
