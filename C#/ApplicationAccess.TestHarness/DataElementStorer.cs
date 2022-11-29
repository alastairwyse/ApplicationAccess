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
using System.Threading;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Stores data elements stored in the AccessManager instance under test.
    /// </summary>
    public class DataElementStorer<TUser, TGroup, TComponent, TAccess>
    {
        protected RandomlyAccessibleSet<TUser> users;
        protected RandomlyAccessibleSet<TGroup> groups;
        protected RandomlyAccessibleDictionary<TUser, RandomlyAccessibleSet<TGroup>> userToGroupMap;
        protected RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleSet<TGroup>> groupToGroupMap;
        protected RandomlyAccessibleDictionary<TUser, RandomlyAccessibleSet<Tuple<TComponent, TAccess>>> userToComponentMap;
        protected RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleSet<Tuple<TComponent, TAccess>>> groupToComponentMap;
        protected RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>> entities;
        protected RandomlyAccessibleDictionary<TUser, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> userToEntityMap;
        protected RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> groupToEntityMap;
        protected Int32 userToGroupMappingCount;
        protected Int32 groupToGroupMappingCount;
        protected Int32 userToComponentMappingCount;
        protected Int32 groupToComponentMappingCount;
        protected Int32 entityCount;
        protected Int32 userToEntityMappingCount;
        protected Int32 groupToEntityMappingCount;
        protected LockManager lockManager;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.DataElementStorer class.
        /// </summary>
        public DataElementStorer()
        {
            users = new RandomlyAccessibleSet<TUser>();
            groups = new RandomlyAccessibleSet<TGroup>();
            userToGroupMap = new RandomlyAccessibleDictionary<TUser, RandomlyAccessibleSet<TGroup>>();
            groupToGroupMap = new RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleSet<TGroup>>();
            userToComponentMap = new RandomlyAccessibleDictionary<TUser, RandomlyAccessibleSet<Tuple<TComponent, TAccess>>>();
            groupToComponentMap = new RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleSet<Tuple<TComponent, TAccess>>>();
            entities = new RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>();
            userToEntityMap = new RandomlyAccessibleDictionary<TUser, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>>();
            groupToEntityMap = new RandomlyAccessibleDictionary<TGroup, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>>();
            userToGroupMappingCount = 0;
            groupToGroupMappingCount = 0;
            userToComponentMappingCount = 0;
            groupToComponentMappingCount = 0;
            entityCount = 0;
            userToEntityMappingCount = 0;
            groupToEntityMappingCount = 0;
            lockManager = new LockManager();
            InitializeLockManager();
        }

        public Int32 UserCount
        {
            get
            {
                Int32 returnCount = 0;
                lockManager.AcquireLocksAndInvokeAction(users, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
                {
                    returnCount = users.Count;
                }));

                return returnCount;
            }
        }

        public Int32 GroupCount
        {
            get
            {
                Int32 returnCount = 0;
                lockManager.AcquireLocksAndInvokeAction(groups, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
                {
                    returnCount = groups.Count;
                }));

                return returnCount;
            }
        }

        public Int32 UserToGroupMappingCount
        {
            get
            {
                return userToGroupMappingCount;
            }
        }

        public Int32 GroupToGroupMappingCount
        {
            get
            {
                return groupToGroupMappingCount;
            }
        }

        public Int32 UserToComponentMappingCount
        {
            get
            {
                return userToComponentMappingCount;
            }
        }

        public Int32 GroupToComponentMappingCount
        {
            get
            {
                return groupToComponentMappingCount;
            }
        }

        public Int32 EntityTypeCount
        {
            get
            {
                Int32 returnCount = 0;
                lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
                {
                    returnCount = entities.Count;
                }));

                return returnCount;
            }
        }

        public Int32 EntityCount
        {
            get
            {
                return entityCount;
            }
        }

        public Int32 UserToEntityMappingCount
        {
            get
            {
                return userToEntityMappingCount;
            }
        }

        public Int32 GroupToEntityMappingCount
        {
            get
            {
                return groupToEntityMappingCount;
            }
        }

        public void AddUser(TUser user)
        {
            lockManager.AcquireLocksAndInvokeAction(users, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddUserIfNotExists(user);
            }));
        }

        public void RemoveUser(TUser user)
        {
            lockManager.AcquireLocksAndInvokeAction(users, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (userToEntityMap.ContainsKey(user) == true)
                {
                    Int32 newUserToEntityMappingCount = userToEntityMappingCount;
                    foreach (KeyValuePair<String, RandomlyAccessibleSet<String>> currentKvp in userToEntityMap[user])
                    {
                        newUserToEntityMappingCount -= currentKvp.Value.Count;
                    }
                    userToEntityMap.Remove(user);
                    Interlocked.CompareExchange(ref userToEntityMappingCount, newUserToEntityMappingCount, userToEntityMappingCount);
                }
                if (userToComponentMap.ContainsKey(user) == true)
                {
                    Int32 newUserToComponentMappingCount = userToComponentMappingCount - userToComponentMap[user].Count;
                    userToComponentMap.Remove(user);
                    Interlocked.CompareExchange(ref userToComponentMappingCount, newUserToComponentMappingCount, userToComponentMappingCount);
                }
                if (userToGroupMap.ContainsKey(user) == true)
                {
                    Int32 newUserToGroupMappingCount = userToGroupMappingCount - userToGroupMap[user].Count;
                    userToGroupMap.Remove(user);
                    Interlocked.CompareExchange(ref userToGroupMappingCount, newUserToGroupMappingCount, userToGroupMappingCount);
                }
                if (users.Contains(user) == true)
                {
                    users.Remove(user);
                }
            }));
        }

        public TUser GetRandomUser()
        {
            TUser returnUser = default;
            lockManager.AcquireLocksAndInvokeAction(users, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnUser = users.GetRandomItem();
            }));

            return returnUser;
        }

        public void AddGroup(TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(groups, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddGroupIfNotExists(group);
            }));
        }

        public void RemoveGroup(TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(groups, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (groupToEntityMap.ContainsKey(group) == true)
                {
                    Int32 newGroupToEntityMappingCount = groupToEntityMappingCount;
                    foreach (KeyValuePair<String, RandomlyAccessibleSet<String>> currentKvp in groupToEntityMap[group])
                    {
                        newGroupToEntityMappingCount -= currentKvp.Value.Count;
                    }
                    groupToEntityMap.Remove(group);
                    Interlocked.CompareExchange(ref groupToEntityMappingCount, newGroupToEntityMappingCount, groupToEntityMappingCount);
                }
                if (groupToComponentMap.ContainsKey(group) == true)
                {
                    Int32 newGroupToComponentMappingCount = groupToComponentMappingCount - groupToComponentMap[group].Count;
                    groupToComponentMap.Remove(group);
                    Interlocked.CompareExchange(ref groupToComponentMappingCount, newGroupToComponentMappingCount, groupToComponentMappingCount);
                }
                foreach (KeyValuePair<TGroup, RandomlyAccessibleSet<TGroup>> currentKvp in groupToGroupMap)
                {
                    if (currentKvp.Value.Contains(group) == true)
                    {
                        currentKvp.Value.Remove(group);
                        Interlocked.Decrement(ref groupToGroupMappingCount);
                    }
                }
                if (groupToGroupMap.ContainsKey(group) == true)
                {
                    Int32 newGroupToGroupMappingCount = groupToGroupMappingCount - groupToGroupMap[group].Count;
                    groupToGroupMap.Remove(group);
                    Interlocked.CompareExchange(ref groupToGroupMappingCount, newGroupToGroupMappingCount, groupToGroupMappingCount);
                }
                foreach (KeyValuePair<TUser, RandomlyAccessibleSet<TGroup>> currentKvp in userToGroupMap)
                {
                    if (currentKvp.Value.Contains(group) == true)
                    {
                        currentKvp.Value.Remove(group);
                        Interlocked.Decrement(ref userToGroupMappingCount);
                    }
                }
                if (groups.Contains(group) == true)
                {
                    groups.Remove(group);
                }
            }));
        }

        public TGroup GetRandomGroup()
        {
            TGroup returnGroup = default;
            lockManager.AcquireLocksAndInvokeAction(groups, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnGroup = groups.GetRandomItem();
            }));

            return returnGroup;
        }

        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddUserIfNotExists(user);
                AddGroupIfNotExists(group);
                if (userToGroupMap.ContainsKey(user) == false)
                {
                    userToGroupMap.Add(user, new RandomlyAccessibleSet<TGroup>());
                }
                if (userToGroupMap[user].Contains(group) == false)
                {
                    userToGroupMap[user].Add(group);
                    Interlocked.Increment(ref userToGroupMappingCount);
                }
            }));
        }

        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (userToGroupMap.ContainsKey(user) == true)
                {
                    if (userToGroupMap[user].Contains(group) == true)
                    {
                        userToGroupMap[user].Remove(group);
                        Interlocked.Decrement(ref userToGroupMappingCount);
                    }
                    if (userToGroupMap[user].Count == 0)
                    {
                        userToGroupMap.Remove(user);
                    }
                }
            }));
        }

        public Tuple<TUser, TGroup> GetRandomUserToGroupMapping()
        {
            TUser returnUser = default;
            TGroup returnGroup = default;
            lockManager.AcquireLocksAndInvokeAction(userToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnUser = userToGroupMap.GetRandomPair().Key;
                returnGroup = userToGroupMap[returnUser].GetRandomItem();
            }));

            return new Tuple<TUser, TGroup>(returnUser, returnGroup);
        }

        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddGroupIfNotExists(fromGroup);
                AddGroupIfNotExists(toGroup);
                if (groupToGroupMap.ContainsKey(fromGroup) == false)
                {
                    groupToGroupMap.Add(fromGroup, new RandomlyAccessibleSet<TGroup>());
                }
                if (groupToGroupMap[fromGroup].Contains(toGroup) == false)
                {
                    groupToGroupMap[fromGroup].Add(toGroup);
                    Interlocked.Increment(ref groupToGroupMappingCount);
                }
            }));
        }

        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (groupToGroupMap.ContainsKey(fromGroup) == true)
                {
                    if (groupToGroupMap[fromGroup].Contains(toGroup) == true)
                    {
                        groupToGroupMap[fromGroup].Remove(toGroup);
                        Interlocked.Decrement(ref groupToGroupMappingCount);
                    }
                    if (groupToGroupMap[fromGroup].Count == 0)
                    {
                        groupToGroupMap.Remove(fromGroup);
                    }
                }
            }));
        }

        public Tuple<TGroup, TGroup> GetRandomGroupToGroupMapping()
        {
            TGroup returnfromGroup = default;
            TGroup returnToGroup = default;
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnfromGroup = groupToGroupMap.GetRandomPair().Key;
                returnToGroup = groupToGroupMap[returnfromGroup].GetRandomItem();
            }));

            return new Tuple<TGroup, TGroup>(returnfromGroup, returnToGroup);
        }

        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddUserIfNotExists(user);
                var tuple = new Tuple<TComponent, TAccess>(applicationComponent, accessLevel);
                if (userToComponentMap.ContainsKey(user) == false)
                {
                    userToComponentMap.Add(user, new RandomlyAccessibleSet<Tuple<TComponent, TAccess>>());
                }
                if (userToComponentMap[user].Contains(tuple) == false)
                {
                    userToComponentMap[user].Add(tuple);
                    Interlocked.Increment(ref userToComponentMappingCount);
                }
            }));
        }

        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (userToComponentMap.ContainsKey(user) == true)
                {
                    var tuple = new Tuple<TComponent, TAccess>(applicationComponent, accessLevel);
                    if (userToComponentMap[user].Contains(tuple) == true)
                    {
                        userToComponentMap[user].Remove(tuple);
                        Interlocked.Decrement(ref userToComponentMappingCount);
                    }
                    if (userToComponentMap[user].Count == 0)
                    {
                        userToComponentMap.Remove(user);
                    }
                }
            }));
        }

        public Tuple<TUser, TComponent, TAccess> GetRandomUserToApplicationComponentAndAccessLevelMapping()
        {
            TUser returnUser = default;
            TComponent returnComponent = default;
            TAccess returnAccessLevel = default;
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnUser = userToComponentMap.GetRandomPair().Key;
                var tuple = userToComponentMap[returnUser].GetRandomItem();
                returnComponent = tuple.Item1;
                returnAccessLevel = tuple.Item2;
            }));

            return new Tuple<TUser, TComponent, TAccess>(returnUser, returnComponent, returnAccessLevel);
        }

        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddGroupIfNotExists(group);
                var tuple = new Tuple<TComponent, TAccess>(applicationComponent, accessLevel);
                if (groupToComponentMap.ContainsKey(group) == false)
                {
                    groupToComponentMap.Add(group, new RandomlyAccessibleSet<Tuple<TComponent, TAccess>>());
                }
                if (groupToComponentMap[group].Contains(tuple) == false)
                {
                    groupToComponentMap[group].Add(tuple);
                    Interlocked.Increment(ref groupToComponentMappingCount);
                }
            }));
        }

        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (groupToComponentMap.ContainsKey(group) == true)
                {
                    var tuple = new Tuple<TComponent, TAccess>(applicationComponent, accessLevel);
                    if (groupToComponentMap[group].Contains(tuple) == true)
                    {
                        groupToComponentMap[group].Remove(tuple);
                        Interlocked.Decrement(ref groupToComponentMappingCount);
                    }
                    if (groupToComponentMap[group].Count == 0)
                    {
                        groupToComponentMap.Remove(group);
                    }
                }
            }));
        }

        public Tuple<TGroup, TComponent, TAccess> GetRandomGroupToApplicationComponentAndAccessLevelMapping()
        {
            TGroup returnGroup = default;
            TComponent returnComponent = default;
            TAccess returnAccessLevel = default;
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnGroup = groupToComponentMap.GetRandomPair().Key;
                var tuple = groupToComponentMap[returnGroup].GetRandomItem();
                returnComponent = tuple.Item1;
                returnAccessLevel = tuple.Item2;
            }));

            return new Tuple<TGroup, TComponent, TAccess>(returnGroup, returnComponent, returnAccessLevel);
        }

        public void AddEntityType(String entityType)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddEntityTypeIfNotExists(entityType);
            }));
        }

        public void RemoveEntityType(String entityType)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                var usersToRemove = new List<TUser>();
                Int32 newUserToEntityMappingCount = userToEntityMappingCount;
                foreach (KeyValuePair<TUser, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> currentKvp in userToEntityMap)
                {
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        newUserToEntityMappingCount -= currentKvp.Value[entityType].Count;
                        currentKvp.Value.Remove(entityType);
                    }
                    if (currentKvp.Value.Count == 0)
                    {
                        usersToRemove.Add(currentKvp.Key);
                    }
                }
                foreach (TUser currentUser in usersToRemove)
                {
                    userToEntityMap.Remove(currentUser);
                }
                Interlocked.CompareExchange(ref userToEntityMappingCount, newUserToEntityMappingCount, userToEntityMappingCount);

                var groupsToRemove = new List<TGroup>();
                Int32 newGroupToEntityMappingCount = groupToEntityMappingCount;
                foreach (KeyValuePair<TGroup, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> currentKvp in groupToEntityMap)
                {
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        newGroupToEntityMappingCount -= currentKvp.Value[entityType].Count;
                        currentKvp.Value.Remove(entityType);
                    }
                    if (currentKvp.Value.Count == 0)
                    {
                        groupsToRemove.Add(currentKvp.Key);
                    }
                }
                foreach (TGroup currentGroup in groupsToRemove)
                {
                    groupToEntityMap.Remove(currentGroup);
                }
                Interlocked.CompareExchange(ref groupToEntityMappingCount, newGroupToEntityMappingCount, groupToEntityMappingCount);

                if (entities.ContainsKey(entityType) == true)
                {
                    Int32 newEntityCount = entityCount - entities[entityType].Count;
                    entities.Remove(entityType);
                    Interlocked.CompareExchange(ref entityCount, newEntityCount, entityCount);
                }
            }));
        }

        public String GetRandomEntityType()
        {
            String returnEntityType = null;
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                returnEntityType = entities.GetRandomPair().Key;
            }));

            return returnEntityType;
        }

        public void AddEntity(String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddEntityTypeIfNotExists(entityType);
                AddEntityIfNotExists(entityType, entity);
            }));
        }

        public void RemoveEntity(String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                var usersToRemove = new List<TUser>();
                foreach (KeyValuePair<TUser, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> currentKvp in userToEntityMap)
                {
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        if (currentKvp.Value[entityType].Contains(entity) == true)
                        {
                            currentKvp.Value[entityType].Remove(entity);
                            Interlocked.Decrement(ref userToEntityMappingCount);
                        }
                    }
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        if (currentKvp.Value[entityType].Count == 0)
                        {
                            currentKvp.Value.Remove(entityType);
                        }
                    }
                    if (currentKvp.Value.Count == 0)
                    {
                        usersToRemove.Add(currentKvp.Key);
                    }
                }
                foreach (TUser currentUser in usersToRemove)
                {
                    userToEntityMap.Remove(currentUser);
                }

                var groupsToRemove = new List<TGroup>();
                foreach (KeyValuePair<TGroup, RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>> currentKvp in groupToEntityMap)
                {
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        if (currentKvp.Value[entityType].Contains(entity) == true)
                        {
                            currentKvp.Value[entityType].Remove(entity);
                            Interlocked.Decrement(ref groupToEntityMappingCount);
                        }
                    }
                    if (currentKvp.Value.ContainsKey(entityType))
                    {
                        if (currentKvp.Value[entityType].Count == 0)
                        {
                            currentKvp.Value.Remove(entityType);
                        }
                    }
                    if (currentKvp.Value.Count == 0)
                    {
                        groupsToRemove.Add(currentKvp.Key);
                    }
                }
                foreach (TGroup currentGroup in groupsToRemove)
                {
                    groupToEntityMap.Remove(currentGroup);
                }

                if (entities.ContainsKey(entityType) == true && entities[entityType].Contains(entity) == true)
                {
                    entities[entityType].Remove(entity);
                    Interlocked.Decrement(ref entityCount);
                }
            }));
        }

        public Tuple<String, String> GetRandomEntity()
        {
            Tuple<String, String> returnTuple = null;
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                String entityType = entities.GetRandomPair().Key;
                String entity = entities[entityType].GetRandomItem();
                returnTuple = new Tuple<String, String>(entityType, entity);
            }));

            return returnTuple;
        }

        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddUserIfNotExists(user);
                AddEntityTypeIfNotExists(entityType);
                AddEntityIfNotExists(entityType, entity);
                if (userToEntityMap.ContainsKey(user) == false)
                {
                    userToEntityMap.Add(user, new RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>());
                }
                if (userToEntityMap[user].ContainsKey(entityType) == false)
                {
                    userToEntityMap[user].Add(entityType, new RandomlyAccessibleSet<String>());
                }
                if (userToEntityMap[user][entityType].Contains(entity) == false)
                {
                    userToEntityMap[user][entityType].Add(entity);
                    Interlocked.Increment(ref userToEntityMappingCount);
                }
            }));
        }

        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true)
                {
                    if (userToEntityMap[user][entityType].Contains(entity) == true)
                    {
                        userToEntityMap[user][entityType].Remove(entity);
                        Interlocked.Decrement(ref userToEntityMappingCount);
                    }
                    if (userToEntityMap[user][entityType].Count == 0)
                    {
                        userToEntityMap[user].Remove(entityType);
                    }
                    if (userToEntityMap[user].Count == 0)
                    {
                        userToEntityMap.Remove(user);
                    }
                }
            }));
        }

        public Tuple<TUser, String, String> GetRandomUserToEntityMapping()
        {
            Tuple<TUser, String, String> returnTuple = null;
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                TUser user = userToEntityMap.GetRandomPair().Key;
                String entityType = userToEntityMap[user].GetRandomPair().Key;
                String entity = userToEntityMap[user][entityType].GetRandomItem();
                returnTuple = new Tuple<TUser, String, String>(user, entityType, entity);
            }));

            return returnTuple;
        }

        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                AddGroupIfNotExists(group);
                AddEntityTypeIfNotExists(entityType);
                AddEntityIfNotExists(entityType, entity);
                if (groupToEntityMap.ContainsKey(group) == false)
                {
                    groupToEntityMap.Add(group, new RandomlyAccessibleDictionary<String, RandomlyAccessibleSet<String>>());
                }
                if (groupToEntityMap[group].ContainsKey(entityType) == false)
                {
                    groupToEntityMap[group].Add(entityType, new RandomlyAccessibleSet<String>());
                }
                if (groupToEntityMap[group][entityType].Contains(entity) == false)
                {
                    groupToEntityMap[group][entityType].Add(entity);
                    Interlocked.Increment(ref groupToEntityMappingCount);
                }
            }));
        }

        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == true)
                {
                    if (groupToEntityMap[group][entityType].Contains(entity) == true)
                    {
                        groupToEntityMap[group][entityType].Remove(entity);
                        Interlocked.Decrement(ref groupToEntityMappingCount);
                    }
                    if (groupToEntityMap[group][entityType].Count == 0)
                    {
                        groupToEntityMap[group].Remove(entityType);
                    }
                    if (groupToEntityMap[group].Count == 0)
                    {
                        groupToEntityMap.Remove(group);
                    }
                }
            }));
        }

        public Tuple<TGroup, String, String> GetRandomGroupToEntityMapping()
        {
            Tuple<TGroup, String, String> returnTuple = null;
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                TGroup group = groupToEntityMap.GetRandomPair().Key;
                String entityType = groupToEntityMap[group].GetRandomPair().Key;
                String entity = groupToEntityMap[group][entityType].GetRandomItem();
                returnTuple = new Tuple<TGroup, String, String>(group, entityType, entity);
            }));

            return returnTuple;
        }

        #region Private/Protected Methods

        protected void InitializeLockManager()
        {
            lockManager.RegisterLockObject(users);
            lockManager.RegisterLockObject(groups);
            lockManager.RegisterLockObject(userToGroupMap);
            lockManager.RegisterLockObject(groupToGroupMap);
            lockManager.RegisterLockObject(userToComponentMap);
            lockManager.RegisterLockObject(groupToComponentMap);
            lockManager.RegisterLockObject(entities);
            lockManager.RegisterLockObject(userToEntityMap);
            lockManager.RegisterLockObject(groupToEntityMap);
            lockManager.RegisterLockObjectDependency(userToGroupMap, users);
            lockManager.RegisterLockObjectDependency(userToGroupMap, groups);
            lockManager.RegisterLockObjectDependency(groupToGroupMap, groups);
            lockManager.RegisterLockObjectDependency(userToComponentMap, users);
            lockManager.RegisterLockObjectDependency(userToEntityMap, users);
            lockManager.RegisterLockObjectDependency(userToEntityMap, entities);
            lockManager.RegisterLockObjectDependency(groupToComponentMap, groups);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, groups);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, entities);   
        }

        protected void AddUserIfNotExists(TUser user)
        {
            if (users.Contains(user) == false)
            {
                users.Add(user);
            }
        }

        protected void AddGroupIfNotExists(TGroup group)
        {
            if (groups.Contains(group) == false)
            {
                groups.Add(group);
            }
        }

        protected void AddEntityTypeIfNotExists(String entityType)
        {
            if (entities.ContainsKey(entityType) == false)
            {
                entities.Add(entityType, new RandomlyAccessibleSet<String>());
            }
        }

        protected void AddEntityIfNotExists(String entityType, String entity)
        {
            if (entities[entityType].Contains(entity) == false)
            {
                entities[entityType].Add(entity);
                Interlocked.Increment(ref entityCount);
            }
        }

        #endregion
    }
}
