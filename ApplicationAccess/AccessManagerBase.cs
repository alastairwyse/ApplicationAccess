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
using System.Collections.Generic;

namespace ApplicationAccess
{
    /// <summary>
    /// A base class for managing the access of users and groups of users to components and entities within an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Note that all generic type parameters must implement relevant methods to allow storing in a <see cref="System.Collections.Generic.HashSet{T}"/> (at minimum <see cref="IEquatable{T}"/> and <see cref="Object.GetHashCode">GetHashcode()</see>).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</remarks>
    public abstract class AccessManagerBase<TUser, TGroup, TComponent, TAccess> : IAccessManager<TUser, TGroup, TComponent, TAccess>
    {        
        /// <summary>Creates instances of collection classes.</summary>
        protected readonly ICollectionFactory collectionFactory;
        /// <summary>The DirectedGraph which stores the user to group mappings.</summary>
        protected readonly DirectedGraphBase<TUser, TGroup> userToGroupMap;
        /// <summary>A dictionary which stores mappings between a user, and application component, and a level of access to that component.</summary>
        protected readonly IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> userToComponentMap;
        /// <summary>A dictionary which stores mappings between a group, and application component, and a level of access to that component.</summary>
        protected readonly IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> groupToComponentMap;
        /// <summary>The reverse of the mappings in member 'userToComponentMap'.</summary>
        protected readonly IDictionary<TComponent, IDictionary<TAccess, ISet<TUser>>> userToComponentReverseMap;
        /// <summary>The reverse of the mappings in member 'groupToComponentMap'.</summary>
        protected readonly IDictionary<TComponent, IDictionary<TAccess, ISet<TGroup>>> groupToComponentReverseMap;
        /// <summary>Holds all valid entity types and values within the access manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).</summary>
        protected readonly IDictionary<String, ISet<String>> entities;
        /// <summary>A dictionary which stores user to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the user.</summary>
        protected readonly IDictionary<TUser, IDictionary<String, ISet<String>>> userToEntityMap;
        /// <summary>A dictionary which stores group to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the group.</summary>
        protected readonly IDictionary<TGroup, IDictionary<String, ISet<String>>> groupToEntityMap;
        /// <summary>The reverse of the mappings in member 'userToEntityMap'.</summary>
        protected readonly IDictionary<String, IDictionary<String, ISet<TUser>>> userToEntityReverseMap;
        /// <summary>The reverse of the mappings in member 'groupToEntityMap'.</summary>
        protected readonly IDictionary<String, IDictionary<String, ISet<TGroup>>> groupToEntityReverseMap;

        /// <inheritdoc/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return userToGroupMap.LeafVertices;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return userToGroupMap.NonLeafVertices;
            }
        }

        /// <inheritdoc/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                return entities.Keys;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.AccessManagerBase class.
        /// </summary>
        /// <param name="collectionFactory">Creates instances of collection classes.</param>
        /// <param name="userToGroupMap">The user to group map the access manager should use internally.</param>
        public AccessManagerBase(ICollectionFactory collectionFactory, DirectedGraphBase<TUser, TGroup> userToGroupMap)
        {
            this.collectionFactory = collectionFactory;
            this.userToGroupMap = userToGroupMap;
            userToComponentMap = this.collectionFactory.GetDictionaryInstance<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>>();
            groupToComponentMap = this.collectionFactory.GetDictionaryInstance<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>>();
            userToComponentReverseMap = this.collectionFactory.GetDictionaryInstance<TComponent, IDictionary<TAccess, ISet<TUser>>>();
            groupToComponentReverseMap = this.collectionFactory.GetDictionaryInstance<TComponent, IDictionary<TAccess, ISet<TGroup>>>();
            entities = this.collectionFactory.GetDictionaryInstance<String, ISet<String>>();
            userToEntityMap = this.collectionFactory.GetDictionaryInstance<TUser, IDictionary<String, ISet<String>>>();
            groupToEntityMap = this.collectionFactory.GetDictionaryInstance<TGroup, IDictionary<String, ISet<String>>>();
            userToEntityReverseMap = this.collectionFactory.GetDictionaryInstance<String, IDictionary<String, ISet<TUser>>>();
            groupToEntityReverseMap = this.collectionFactory.GetDictionaryInstance<String, IDictionary<String, ISet<TGroup>>>();
        }

        /// <summary>
        /// Removes all items and mappings from the access manager.
        /// </summary>
        /// <remarks>Since the Clear() method on HashSets and Dictionaries underlying the class are O(n) operations, performance will scale roughly with the number of items and mappings stored in the access manager.</remarks>
        public virtual void Clear()
        {
            userToGroupMap.Clear();
            userToComponentMap.Clear();
            groupToComponentMap.Clear();
            userToComponentReverseMap.Clear();
            groupToComponentReverseMap.Clear();
            entities.Clear();
            userToEntityMap.Clear();
            groupToEntityMap.Clear();
            userToEntityReverseMap.Clear();
            groupToEntityReverseMap.Clear();
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">The specified user already exists.</exception>
        public virtual void AddUser(TUser user)
        {
            try
            {
                userToGroupMap.AddLeafVertex(user);
            }
            catch (LeafVertexAlreadyExistsException<TUser> e)
            {
                throw new ArgumentException($"User '{user.ToString()}' already exists.", nameof(user), e);
            }
        }

        /// <inheritdoc/>
        public virtual Boolean ContainsUser(TUser user)
        {
            return userToGroupMap.ContainsLeafVertex(user);
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual void RemoveUser(TUser user)
        {
            try
            {
                userToGroupMap.RemoveLeafVertex(user);
            }
            catch (LeafVertexNotFoundException<TUser> e)
            {
                ThrowUserDoesntExistException(user, nameof(user), e);
            }
            if (userToComponentMap.ContainsKey(user) == true)
            {
                foreach(ApplicationComponentAndAccessLevel<TComponent, TAccess> currentApplicationComponentAndAccessLevel in userToComponentMap[user])
                {
                    userToComponentReverseMap[currentApplicationComponentAndAccessLevel.ApplicationComponent][currentApplicationComponentAndAccessLevel.AccessLevel].Remove(user);
                }
                userToComponentMap.Remove(user);
            }
            if (userToEntityMap.ContainsKey(user) == true)
            {
                foreach (String currentEntityType in userToEntityMap[user].Keys)
                {
                    foreach (String currentEntity in userToEntityMap[user][currentEntityType])
                    {
                        userToEntityReverseMap[currentEntityType][currentEntity].Remove(user);
                    }
                }
                userToEntityMap.Remove(user);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">The specified group already exists.</exception>
        public virtual void AddGroup(TGroup group)
        {
            try
            {
                userToGroupMap.AddNonLeafVertex(group);
            }
            catch (NonLeafVertexAlreadyExistsException<TUser> e)
            {
                throw new ArgumentException($"Group '{group.ToString()}' already exists.", nameof(group), e);
            }
        }

        /// <inheritdoc/>
        public virtual Boolean ContainsGroup(TGroup group)
        {
            return userToGroupMap.ContainsNonLeafVertex(group);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual void RemoveGroup(TGroup group)
        {
            try
            {
                userToGroupMap.RemoveNonLeafVertex(group);
            }
            catch (NonLeafVertexNotFoundException<TGroup> e)
            {
                ThrowGroupDoesntExistException(group, nameof(group), e);
            }
            if (groupToComponentMap.ContainsKey(group) == true)
            {
                foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentApplicationComponentAndAccessLevel in groupToComponentMap[group])
                {
                    groupToComponentReverseMap[currentApplicationComponentAndAccessLevel.ApplicationComponent][currentApplicationComponentAndAccessLevel.AccessLevel].Remove(group);
                }
                groupToComponentMap.Remove(group);
            }
            if (groupToEntityMap.ContainsKey(group) == true)
            {
                foreach (String currentEntityType in groupToEntityMap[group].Keys)
                {
                    foreach (String currentEntity in groupToEntityMap[group][currentEntityType])
                    {
                        groupToEntityReverseMap[currentEntityType][currentEntity].Remove(group);
                    }
                }
                groupToEntityMap.Remove(group);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user and group already exists.</exception>
        public virtual void AddUserToGroupMapping(TUser user, TGroup group)
        {
            try
            {
                userToGroupMap.AddLeafToNonLeafEdge(user, group);
            }
            catch (LeafVertexNotFoundException<TUser> leafVertexNotFoundException)
            {
                ThrowUserDoesntExistException(user, nameof(user), leafVertexNotFoundException);
            }
            catch (NonLeafVertexNotFoundException<TGroup> nonLeafVertexNotFoundException)
            {
                ThrowGroupDoesntExistException(group, nameof(group), nonLeafVertexNotFoundException);
            }
            catch (LeafToNonLeafEdgeAlreadyExistsException<TUser, TGroup> leafToNonLeafEdgeAlreadyExistsException)
            {
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and group '{group.ToString()}' already exists.", nameof(group), leafToNonLeafEdgeAlreadyExistsException);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            var returnGroups = new HashSet<TGroup>();
            try
            {
                returnGroups.UnionWith(userToGroupMap.GetLeafEdges(user));
            }
            catch (LeafVertexNotFoundException<TUser> e)
            {
                ThrowUserDoesntExistException(user, nameof(user), e);
            }
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
                {
                    if (returnGroups.Contains(currentGroup) == false)
                    {
                        returnGroups.Add(currentGroup);
                    }

                    return true;
                };
                userToGroupMap.TraverseFromLeaf(user, vertexAction);
            }

            return returnGroups;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            var returnUsers = new HashSet<TUser>();
            try
            {
                returnUsers.UnionWith(userToGroupMap.GetLeafReverseEdges(group));
            }
            catch (NonLeafVertexNotFoundException<TGroup> e)
            {
                ThrowGroupDoesntExistException(group, nameof(group), e);
            }
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> nonLeafvertexAction = (TGroup currentGroup) => { return true; };
                Func<TUser, Boolean> leafVertexAction = (TUser currentUser) =>
                {
                    if (returnUsers.Contains(currentUser) == false)
                    {
                        returnUsers.Add(currentUser);
                    }

                    return true;
                };
                
                // It's possbile that group could have been removed by this point due to time taken to do the above retrieval
                try
                {
                    userToGroupMap.TraverseReverseFromNonLeaf(group, nonLeafvertexAction, leafVertexAction);
                }
                catch (NonLeafVertexNotFoundException<TGroup>)
                {
                }
            }

            return returnUsers;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user and group does not exist.</exception>
        public virtual void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            try
            {
                userToGroupMap.RemoveLeafToNonLeafEdge(user, group);
            }
            catch (LeafVertexNotFoundException<TUser> leafVertexNotFoundException)
            {
                ThrowUserDoesntExistException(user, nameof(user), leafVertexNotFoundException);
            }
            catch (NonLeafVertexNotFoundException<TGroup> nonLeafVertexNotFoundException)
            {
                ThrowGroupDoesntExistException(group, nameof(group), nonLeafVertexNotFoundException);
            }
            catch (LeafToNonLeafEdgeNotFoundException<TUser, TGroup> leafToNonLeafEdgeNotFoundException)
            {
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and group '{group.ToString()}' does not exist.", nameof(group), leafToNonLeafEdgeNotFoundException);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="ArgumentException">The 'from' and 'to' groups cannot contain the same group.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified groups already exists.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified groups cannot be created as it would cause a circular reference.</exception>
        public virtual void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            if (userToGroupMap.ContainsNonLeafVertex(fromGroup) == false)
                ThrowGroupDoesntExistException(fromGroup, nameof(fromGroup));
            if (userToGroupMap.ContainsNonLeafVertex(toGroup) == false)
                ThrowGroupDoesntExistException(toGroup, nameof(toGroup));
            if (fromGroup.Equals(toGroup) == true)
                throw new ArgumentException($"Parameters '{nameof(fromGroup)}' and '{nameof(toGroup)}' cannot contain the same group.", nameof(toGroup));

            try
            {
                userToGroupMap.AddNonLeafToNonLeafEdge(fromGroup, toGroup);
            }
            catch (NonLeafToNonLeafEdgeAlreadyExistsException<TGroup> nonLeafToNonLeafEdgeAlreadyExistsException)
            {
                throw new ArgumentException($"A mapping between groups '{fromGroup.ToString()}' and '{toGroup.ToString()}' already exists.", nameof(toGroup), nonLeafToNonLeafEdgeAlreadyExistsException);
            }
            catch (CircularReferenceException circularReferenceException)
            {
                throw new ArgumentException($"A mapping between groups '{fromGroup.ToString()}' and '{toGroup.ToString()}' cannot be created as it would cause a circular reference.", nameof(toGroup), circularReferenceException);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var returnGroups = new HashSet<TGroup>(userToGroupMap.GetNonLeafEdges(group));
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
                {
                    if ((returnGroups.Contains(currentGroup) == false) && (currentGroup.Equals(group) == false))
                    {
                        returnGroups.Add(currentGroup);
                    }

                    return true;
                };
                userToGroupMap.TraverseFromNonLeaf(group, vertexAction);
            }

            return returnGroups;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            var returnGroups = new HashSet<TGroup>();
            try
            {
                returnGroups.UnionWith(userToGroupMap.GetNonLeafReverseEdges(group));
            }
            catch (NonLeafVertexNotFoundException<TGroup> e)
            {
                ThrowGroupDoesntExistException(group, nameof(group), e);
            }
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> nonLeafvertexAction = (TGroup currentGroup) =>
                {
                    if (group.Equals(currentGroup) == false)
                    {
                        if (returnGroups.Contains(currentGroup) == false)
                        {
                            returnGroups.Add(currentGroup);
                        }
                    }

                    return true;
                };
                Func<TUser, Boolean> leafVertexAction = (TUser currentUser) => { return true; };
                // It's possbile that group could have been removed by this point due to time taken to do the above retrieval
                try
                {
                    userToGroupMap.TraverseReverseFromNonLeaf(group, nonLeafvertexAction, leafVertexAction);
                }
                catch (NonLeafVertexNotFoundException<TGroup>)
                {
                }
            }

            return returnGroups;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified groups does not exist.</exception>
        public virtual void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            if (userToGroupMap.ContainsNonLeafVertex(fromGroup) == false)
                ThrowGroupDoesntExistException(fromGroup, nameof(fromGroup));
            if (userToGroupMap.ContainsNonLeafVertex(toGroup) == false)
                ThrowGroupDoesntExistException(toGroup, nameof(toGroup));

            try
            {
                userToGroupMap.RemoveNonLeafToNonLeafEdge(fromGroup, toGroup);
            }
            catch (NonLeafToNonLeafEdgeNotFoundException<TGroup> nonLeafToNonLeafEdgeNotFoundException)
            {
                throw new ArgumentException($"A mapping between groups '{fromGroup.ToString()}' and '{toGroup.ToString()}' does not exist.", nameof(toGroup), nonLeafToNonLeafEdgeNotFoundException);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user, application component, and access level already exists.</exception>
        public virtual void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var componentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            if (userToComponentMap.ContainsKey(user) == true)
            {
                if (userToComponentMap[user].Contains(componentAndAccess) == true)
                    throw new ArgumentException($"A mapping between user '{user.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' already exists.");
            }
            else
            {
                userToComponentMap.Add(user, collectionFactory.GetSetInstance<ApplicationComponentAndAccessLevel<TComponent, TAccess>>());
            }
            userToComponentMap[user].Add(componentAndAccess);
            if (userToComponentReverseMap.ContainsKey(applicationComponent) == false)
            {
                userToComponentReverseMap.Add(applicationComponent, collectionFactory.GetDictionaryInstance<TAccess, ISet<TUser>>());
            }
            if (userToComponentReverseMap[applicationComponent].ContainsKey(accessLevel) == false)
            {
                userToComponentReverseMap[applicationComponent].Add(accessLevel, collectionFactory.GetSetInstance<TUser>());
            }
            userToComponentReverseMap[applicationComponent][accessLevel].Add(user);
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            Boolean containsUser = userToComponentMap.TryGetValue(user, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
            if (containsUser == true)
            {
                foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentPair in componentsAndAccessInMapping)
                {
                    yield return new Tuple<TComponent, TAccess>(currentPair.ApplicationComponent, currentPair.AccessLevel);
                }
            }
        }

        /// <inheritdoc/>
        public virtual IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            var returnUsers = new HashSet<TUser>();
            GetApplicationComponentAndAccessLevelReverseMappings(userToComponentReverseMap, applicationComponent, accessLevel, returnUsers);
            if (includeIndirectMappings == true)
            {
                IEnumerable<TGroup> mappedGroups = GetApplicationComponentAndAccessLevelToGroupMappingsImplementation(applicationComponent, accessLevel, true);
                foreach (TGroup currentMappedGroup in mappedGroups)
                {
                    try
                    {
                        returnUsers.UnionWith(userToGroupMap.GetLeafReverseEdges(currentMappedGroup));
                    }
                    catch (NonLeafVertexNotFoundException<TGroup>)
                    {
                        // GetLeafReverseEdges() will throw a NonLeafVertexNotFoundException<TGroup> if the specified group doesn't exist which could happen if another thread deletes the group during traversal
                    }
                }
            }

            return returnUsers;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user, application component, and access level does not exist.</exception>
        public virtual void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var componentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            if (userToComponentMap.ContainsKey(user) == true && userToComponentMap[user].Contains(componentAndAccess) == true)
            {
                userToComponentMap[user].Remove(componentAndAccess);
                if (userToComponentMap[user].Count == 0)
                {
                    userToComponentMap.Remove(user);
                }
            }
            else
            {
                throw new ArgumentException($"A mapping between user '{user.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' doesn't exist.");
            }
            userToComponentReverseMap[applicationComponent][accessLevel].Remove(user);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified group, application component, and access level already exists.</exception>
        public virtual void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var componentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            if (groupToComponentMap.ContainsKey(group) == true)
            {
                if (groupToComponentMap[group].Contains(componentAndAccess) == true)
                    throw new ArgumentException($"A mapping between group '{group.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' already exists.");
            }
            else
            {
                groupToComponentMap.Add(group, collectionFactory.GetSetInstance<ApplicationComponentAndAccessLevel<TComponent, TAccess>>());
            }
            groupToComponentMap[group].Add(componentAndAccess);
            if (groupToComponentReverseMap.ContainsKey(applicationComponent) == false)
            {
                groupToComponentReverseMap.Add(applicationComponent, collectionFactory.GetDictionaryInstance<TAccess, ISet<TGroup>>());
            }
            if (groupToComponentReverseMap[applicationComponent].ContainsKey(accessLevel) == false)
            {
                groupToComponentReverseMap[applicationComponent].Add(accessLevel, collectionFactory.GetSetInstance<TGroup>());
            }
            groupToComponentReverseMap[applicationComponent][accessLevel].Add(group);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified user does not exist.</exception>
        public virtual IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            Boolean containsGroup = groupToComponentMap.TryGetValue(group, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
            if (containsGroup == true)
            {
                foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentPair in componentsAndAccessInMapping)
                {
                    yield return new Tuple<TComponent, TAccess>(currentPair.ApplicationComponent, currentPair.AccessLevel);
                }
            }
        }

        /// <inheritdoc/>
        public virtual IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return GetApplicationComponentAndAccessLevelToGroupMappingsImplementation(applicationComponent, accessLevel, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified user does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified group, application component, and access level does not exist.</exception>
        public virtual void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var componentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            if (groupToComponentMap.ContainsKey(group) == true && groupToComponentMap[group].Contains(componentAndAccess) == true)
            {
                groupToComponentMap[group].Remove(componentAndAccess);
                if (groupToComponentMap[group].Count == 0)
                {
                    groupToComponentMap.Remove(group);
                }
            }
            else
            {
                throw new ArgumentException($"A mapping between group '{group.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' doesn't exist.");
            }
            groupToComponentReverseMap[applicationComponent][accessLevel].Remove(group);
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">The specified entity type already exists.</exception>
        /// <exception cref="ArgumentException">The entity type must contain a valid character.</exception>
        public virtual void AddEntityType(String entityType)
        {
            if (entities.ContainsKey(entityType) == true)
                throw new ArgumentException($"Entity type '{entityType}' already exists.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityType) == true)
                throw new ArgumentException($"Entity type '{entityType}' must contain a valid character.", nameof(entityType));

            entities.Add(entityType, collectionFactory.GetSetInstance<String>());
        }

        /// <inheritdoc/>
        public virtual Boolean ContainsEntityType(String entityType)
        {
            return entities.ContainsKey(entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual void RemoveEntityType(String entityType)
        {
            this.RemoveEntityType(entityType, (actionUser, actionEntityType, actionEntities, actionEntityCount) => { }, (actionGroup, actionEntityType, actionEntities, actionEntityCount) => { });
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="ArgumentException"> The specified entity already exists.</exception>
        /// <exception cref="ArgumentException"> The entity must contain a valid character.</exception>
        public virtual void AddEntity(String entityType, String entity)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == true)
                throw new ArgumentException($"Entity '{entity}' already exists.", nameof(entity));
            if (String.IsNullOrWhiteSpace(entity) == true)
                throw new ArgumentException($"Entity '{entity}' must contain a valid character.", nameof(entity));

            entities[entityType].Add(entity);
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual IEnumerable<String> GetEntities(String entityType)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            Boolean containsEntityType = entities.TryGetValue(entityType, out ISet<String> entitiesOfType);
            if (containsEntityType == true)
            {
                foreach (String currentEntity in entitiesOfType)
                {
                    yield return currentEntity;
                }
            }
        }

        /// <inheritdoc/>
        public virtual Boolean ContainsEntity(String entityType, String entity)
        {
            Boolean containsEntityType = entities.TryGetValue(entityType, out ISet<String> entitiesOfType);
            return (containsEntityType && entitiesOfType.Contains(entity));
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        public virtual void RemoveEntity(String entityType, String entity)
        {
            this.RemoveEntity(entityType, entity, (actionUser, actionEntityType, actionEntity) => { }, (actionGroup, actionEntityType, actionEntity) => { });
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user and entity already exists.</exception>
        public virtual void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true && userToEntityMap[user][entityType].Contains(entity) == true)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' already exists.");

            if (userToEntityMap.ContainsKey(user) == false)
            {
                userToEntityMap.Add(user, collectionFactory.GetDictionaryInstance<String, ISet<String>>());
            }
            if (userToEntityMap[user].ContainsKey(entityType) == false)
            {
                userToEntityMap[user].Add(entityType, collectionFactory.GetSetInstance<String>());
            }
            userToEntityMap[user][entityType].Add(entity);
            if (userToEntityReverseMap.ContainsKey(entityType) == false)
            {
                userToEntityReverseMap.Add(entityType, collectionFactory.GetDictionaryInstance<String, ISet<TUser>>());
            }
            if (userToEntityReverseMap[entityType].ContainsKey(entity) == false)
            {
                userToEntityReverseMap[entityType].Add(entity, collectionFactory.GetSetInstance<TUser>());
            }
            userToEntityReverseMap[entityType][entity].Add(user);
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return GetUserToEntityMappingsImplementation(user);
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return GetUserToEntityMappingsImplementation(user, entityType);
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        public virtual IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (EntityExists(entityType, entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));

            var returnUsers = new HashSet<TUser>();
            GetEntityReverseMappings(userToEntityReverseMap, entityType, entity, returnUsers);
            if (includeIndirectMappings == true)
            {
                IEnumerable<TGroup> mappedGroups = GetEntityToGroupMappingsImplementation(entityType, entity, true);
                foreach (TGroup currentMappedGroup in mappedGroups)
                {
                    try
                    {
                        returnUsers.UnionWith(userToGroupMap.GetLeafReverseEdges(currentMappedGroup));
                    }
                    catch (NonLeafVertexNotFoundException<TGroup>)
                    {
                        // GetLeafReverseEdges() will throw a NonLeafVertexNotFoundException<TGroup> if the specified group doesn't exist which could happen if another thread deletes the group during traversal
                    }
                }
            }

            return returnUsers;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified user and entity does not exist.</exception>
        public virtual void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));
            if (userToEntityMap.ContainsKey(user) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true && userToEntityMap[user][entityType].Contains(entity) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");

            userToEntityMap[user][entityType].Remove(entity);
            userToEntityReverseMap[entityType][entity].Remove(user);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified user does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified group and entity already exists.</exception>
        public virtual void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));
            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == true && groupToEntityMap[group][entityType].Contains(entity) == true)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' already exists.");

            if (groupToEntityMap.ContainsKey(group) == false)
            {
                groupToEntityMap.Add(group, collectionFactory.GetDictionaryInstance<String, ISet<String>>());
            }
            if (groupToEntityMap[group].ContainsKey(entityType) == false)
            {
                groupToEntityMap[group].Add(entityType, collectionFactory.GetSetInstance<String>());
            }
            groupToEntityMap[group][entityType].Add(entity);
            if (groupToEntityReverseMap.ContainsKey(entityType) == false)
            {
                groupToEntityReverseMap.Add(entityType, collectionFactory.GetDictionaryInstance<String, ISet<TGroup>>());
            }
            if (groupToEntityReverseMap[entityType].ContainsKey(entity) == false)
            {
                groupToEntityReverseMap[entityType].Add(entity, collectionFactory.GetSetInstance<TGroup>());
            }
            groupToEntityReverseMap[entityType][entity].Add(group);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return GetGroupToEntityMappingsImplementation(group);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            Boolean containsGroup = groupToEntityMap.TryGetValue(group, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
            if (containsGroup == true)
            {
                Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                if (containsEntity == true)
                {
                    foreach (String currentEntity in entitiesInMapping)
                    {
                        yield return currentEntity;
                    }
                }
            }
        }

        /// <inheritdoc/>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        public virtual IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return GetEntityToGroupMappingsImplementation(entityType, entity, includeIndirectMappings);
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        /// <exception cref="EntityNotFoundException">The specified entity does not exist.</exception>
        /// <exception cref="ArgumentException">A mapping between the specified group and entity does not exist.</exception>
        public virtual void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));
            if (groupToEntityMap.ContainsKey(group) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == true && groupToEntityMap[group][entityType].Contains(entity) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");

            groupToEntityMap[group][entityType].Remove(entity); 
            groupToEntityReverseMap[entityType][entity].Remove(group);
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                return false;
            }
            var comparisonComponentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            Boolean containsUser = userToComponentMap.TryGetValue(user, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
            if (containsUser == true && componentsAndAccessInMapping.Contains(comparisonComponentAndAccess) == true)
            {
                return true;
            }
            Boolean hasAccess = false;
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                containsUser = groupToComponentMap.TryGetValue(currentGroup, out componentsAndAccessInMapping);
                if (containsUser == true && componentsAndAccessInMapping.Contains(comparisonComponentAndAccess) == true)
                {
                    hasAccess = true;
                    return false;
                }
                else
                {
                    return true;
                }
            };
            // It's possbile that user could have been removed by this point due to time taken to do above lookups
            try
            {
                userToGroupMap.TraverseFromLeaf(user, vertexAction);
            }
            catch (LeafVertexNotFoundException<TUser>)
            {
                return hasAccess;
            }

            return hasAccess;
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                return false;
            }
            Boolean containsEntityType = entities.TryGetValue(entityType, out ISet<String> entitiesOfType);
            if (containsEntityType == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entitiesOfType.Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));
            Boolean containsUser = userToEntityMap.TryGetValue(user, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
            if (containsUser == true)
            {
                Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                if (containsEntity == true && entitiesInMapping.Contains(entity) == true)
                {
                    return true;
                }
            }
            Boolean hasAccess = false;
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out entitiesAndTypesInMapping);
                if (containsGroup == true)
                {
                    Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                    if (containsEntity == true && entitiesInMapping.Contains(entity) == true)
                    {
                        hasAccess = true;
                        return false;
                    }
                }

                return true;
            };
            // It's possbile that user could have been removed by this point due to time taken to do above lookups
            try
            {
                userToGroupMap.TraverseFromLeaf(user, vertexAction);
            }
            catch (LeafVertexNotFoundException<TUser>)
            {
                return hasAccess;
            }

            return hasAccess;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var returnComponentsAndAccessLevels = new HashSet<Tuple<TComponent, TAccess>>();
            Boolean containsUser = userToComponentMap.TryGetValue(user, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
            if (containsUser == true)
            {
                foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentComponentAndAccessLevel in componentsAndAccessInMapping)
                {
                    returnComponentsAndAccessLevels.Add(new Tuple<TComponent, TAccess>(currentComponentAndAccessLevel.ApplicationComponent, currentComponentAndAccessLevel.AccessLevel));
                }
            }
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                Boolean containsGroup = groupToComponentMap.TryGetValue(currentGroup, out componentsAndAccessInMapping);
                if (containsGroup == true)
                {
                    foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentComponentAndAccessLevel in componentsAndAccessInMapping)
                    {
                        var currentComponentAndAccessLevelAsTuple = new Tuple<TComponent, TAccess>(currentComponentAndAccessLevel.ApplicationComponent, currentComponentAndAccessLevel.AccessLevel);
                        if (returnComponentsAndAccessLevels.Contains(currentComponentAndAccessLevelAsTuple) == false)
                        {
                            returnComponentsAndAccessLevels.Add(currentComponentAndAccessLevelAsTuple);
                        }
                    }
                }

                return true;
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return returnComponentsAndAccessLevels;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var returnComponentsAndAccessLevels = new HashSet<Tuple<TComponent, TAccess>>();
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                Boolean containsGroup = groupToComponentMap.TryGetValue(currentGroup, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
                if (containsGroup == true)
                {
                    foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentComponentAndAccessLevel in componentsAndAccessInMapping)
                    {
                        var currentComponentAndAccessLevelAsTuple = new Tuple<TComponent, TAccess>(currentComponentAndAccessLevel.ApplicationComponent, currentComponentAndAccessLevel.AccessLevel);
                        if (returnComponentsAndAccessLevels.Contains(currentComponentAndAccessLevelAsTuple) == false)
                        {
                            returnComponentsAndAccessLevels.Add(currentComponentAndAccessLevelAsTuple);
                        }
                    }
                }

                return true;
            };
            userToGroupMap.TraverseFromNonLeaf(group, vertexAction);

            return returnComponentsAndAccessLevels;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        public virtual HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var returnEntities = new HashSet<Tuple<String, String>>();
            try
            {
                IEnumerable<Tuple<String, String>> entitiesAndTypesInMapping = GetUserToEntityMappingsImplementation(user);
                foreach (Tuple<String, String> currentEntity in entitiesAndTypesInMapping)
                {
                    returnEntities.Add(currentEntity);
                }
            }
            catch (ArgumentException)
            {
                // GetUserToEntityMappings() will throw an ArgumentException if the specified user doesn't exist which could happen if another thread deletes the user between the check at the top of this method and here
                return returnEntities;
            }
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                try
                {
                    returnEntities.UnionWith(GetGroupToEntityMappingsImplementation(currentGroup));
                }
                catch (ArgumentException)
                {
                    // GetGroupToEntityMappings() will throw an ArgumentException if the specified group doesn't exist which could happen if another thread deletes the group during traversal
                }

                return true;
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return returnEntities;
        }

        /// <inheritdoc/>
        /// <exception cref="UserNotFoundException{TUser}">The specified user does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            var returnEntities = new HashSet<String>();
            try
            {
                IEnumerable<String> entitiesInMapping = GetUserToEntityMappingsImplementation(user, entityType);
                foreach (String currentEntity in entitiesInMapping)
                {
                    returnEntities.Add(currentEntity);
                }
            }
            catch (ArgumentException)
            {
                // GetUserToEntityMappings() will throw an ArgumentException if the specified user and/or entity type doesn't exist which could happen if another thread deletes either between the check at the top of this method and here
                return returnEntities;
            }
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
                if (containsGroup == true)
                {
                    Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                    if (containsEntity == true)
                    {
                        returnEntities.UnionWith(entitiesInMapping);
                    }
                }

                return true;
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return returnEntities;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        public virtual HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var returnEntities = new HashSet<Tuple<String, String>>();
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                try
                {
                    returnEntities.UnionWith(GetGroupToEntityMappingsImplementation(currentGroup));
                }
                catch (ArgumentException)
                {
                    // GetGroupToEntityMappings() will throw an ArgumentException if the specified group doesn't exist which could happen if another thread deletes the group during traversal
                }

                return true;
            };
            userToGroupMap.TraverseFromNonLeaf(group, vertexAction);

            return returnEntities;
        }

        /// <inheritdoc/>
        /// <exception cref="GroupNotFoundException{TGroup}">The specified group does not exist.</exception>
        /// <exception cref="EntityTypeNotFoundException">The specified entity type does not exist.</exception>
        public virtual HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            var returnEntities = new HashSet<String>();
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
                if (containsGroup == true)
                {
                    Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                    if (containsEntity == true)
                    {
                        returnEntities.UnionWith(entitiesInMapping);
                    }
                }

                return true;
            };
            userToGroupMap.TraverseFromNonLeaf(group, vertexAction);

            return returnEntities;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Checks whether the specified entity exists in a multi-thread-safe manner.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to check.</param>
        /// <returns>True if the entity exists.  False otherwise.</returns>
        protected Boolean EntityExists(String entityType, String entity)
        {
            Boolean entityTypeExists = entities.TryGetValue(entityType, out ISet<String> entitiesForType);
            {
                if (entityTypeExists == true)
                {
                    return entitiesForType.Contains(entity);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="userToEntityTypeMappingPreRemovalAction">An action which is invoked before removing the entity type mappings for a user.  Accepts 4 parameters: the user in the mappings, the type of the entity of the mappings being removed, the entities in the mappings, and the number of entities in the mappings.</param>
        /// <param name="groupToEntityTypeMappingPreRemovalAction">An action which is invoked before removing the entity type mappings for a group.  Accepts 4 parameters: the group in the mappings, the type of the entity of the mappings being removed, the entities in the mappings, and the number of entities in the mappings.</param>
        protected virtual void RemoveEntityType(String entityType, Action<TUser, String, IEnumerable<String>, Int32> userToEntityTypeMappingPreRemovalAction, Action<TGroup, String, IEnumerable<String>, Int32> groupToEntityTypeMappingPreRemovalAction)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            if (userToEntityReverseMap.ContainsKey(entityType)  == true)
            {
                var usersMappedToEntityType = new HashSet<TUser>();
                foreach (String currentEntity in userToEntityReverseMap[entityType].Keys)
                {
                    // TODO: Performance could still be impoved here, as we're iterating all entities of the entity type we're deleting,  to obtain a consolidated set of users mapped from the entity type (and 'through' the entities)
                    //   This is an improvement on previous code where 'userToEntityReverseMap' member didn't exist, but could still be improved potentially by storing another 'reverse map' from entity type directly to user
                    usersMappedToEntityType.UnionWith(userToEntityReverseMap[entityType][currentEntity]);
                }
                foreach (TUser currentUser in usersMappedToEntityType)
                {
                    userToEntityTypeMappingPreRemovalAction.Invoke(currentUser, entityType, userToEntityMap[currentUser][entityType], userToEntityMap[currentUser][entityType].Count);
                    userToEntityMap[currentUser].Remove(entityType);
                }
                userToEntityReverseMap.Remove(entityType);
            }
            if (groupToEntityReverseMap.ContainsKey(entityType) == true)
            {
                var groupsMappedToEntityType = new HashSet<TGroup>();
                foreach (String currentEntity in groupToEntityReverseMap[entityType].Keys)
                {
                    groupsMappedToEntityType.UnionWith(groupToEntityReverseMap[entityType][currentEntity]);
                }
                foreach (TGroup currentGroup in groupsMappedToEntityType)
                {
                    groupToEntityTypeMappingPreRemovalAction.Invoke(currentGroup, entityType, groupToEntityMap[currentGroup][entityType], groupToEntityMap[currentGroup][entityType].Count);
                    groupToEntityMap[currentGroup].Remove(entityType);
                }
                groupToEntityReverseMap.Remove(entityType);
            }

            entities.Remove(entityType);
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="userToEntityMappingPostRemovalAction">An action which is invoked after removing a user to entity mapping.  Accepts 3 parameters: the user in the mapping, the type of the entity in the mapping, and the entity in the mapping.</param>
        /// <param name="groupToEntityMappingPostRemovalAction">An action which is invoked after removing a group to entity mapping.  Accepts 3 parameters: the group in the mapping, the type of the entity in the mapping, and the entity in the mapping.</param>
        protected virtual void RemoveEntity(String entityType, String entity, Action<TUser, String, String> userToEntityMappingPostRemovalAction, Action<TGroup, String, String> groupToEntityMappingPostRemovalAction)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));

            if (userToEntityReverseMap.ContainsKey(entityType) && userToEntityReverseMap[entityType].ContainsKey(entity))
            {
                foreach (TUser currentUser in userToEntityReverseMap[entityType][entity])
                {
                    userToEntityMap[currentUser][entityType].Remove(entity);
                    userToEntityMappingPostRemovalAction.Invoke(currentUser, entityType, entity);
                }
                userToEntityReverseMap[entityType].Remove(entity);
            }
            if (groupToEntityReverseMap.ContainsKey(entityType) && groupToEntityReverseMap[entityType].ContainsKey(entity))
            {
                foreach (TGroup currentGroup in groupToEntityReverseMap[entityType][entity])
                {
                    groupToEntityMap[currentGroup][entityType].Remove(entity);
                    groupToEntityMappingPostRemovalAction.Invoke(currentGroup, entityType, entity);
                }
                groupToEntityReverseMap[entityType].Remove(entity);
            }

            entities[entityType].Remove(entity);
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a group is mapped to an application component and access level via other groups).</param>
        /// <returns>A collection of groups that are mapped to the specified application component and access level.</returns>
        /// <remarks>Putting implementation of GetApplicationComponentAndAccessLevelToGroupMappings() in this method allows methods like GetApplicationComponentAndAccessLevelToUserMappings() to call this method rather than the public virtual GetApplicationComponentAndAccessLevelToGroupMappings().  Calling the public virtual version from GetApplicationComponentAndAccessLevelToGroupMappings() can result is overridden implementations being called, and hence unexpected behaviour (specifically metrics being logged unnecessarily).</remarks>
        protected IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappingsImplementation(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            var returnGroups = new HashSet<TGroup>();
            GetApplicationComponentAndAccessLevelReverseMappings(groupToComponentReverseMap, applicationComponent, accessLevel, returnGroups);
            var visitedGroups = new HashSet<TGroup>();
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> nonLeafvertexAction = (TGroup currentGroup) =>
                {
                    // TODO: There's some inefficency here because we don't have a way to stop traverrsing further forward but continue traversing 'sideways' within the TraverseReverseFromNonLeaf() method
                    //   If we return false from this Func it will stop all traversal all further traversal which is not what we want here
                    //   i.e. we still want to traverse to groups with the same parent/source as current, but just want to stop further traversal from 'currentGroup'
                    //   as stated, TraverseReverseFromNonLeaf() doesn't support that
                    if (visitedGroups.Contains(currentGroup) == false)
                    {
                        returnGroups.Add(currentGroup);
                        visitedGroups.Add(currentGroup);
                    }

                    return true;
                };
                Func<TUser, Boolean> leafVertexAction = (TUser currentUser) => { return true; };
                var directlyMappedGroups = new List<TGroup>(returnGroups);
                foreach (TGroup currentMappedGroup in directlyMappedGroups)
                {
                    if (visitedGroups.Contains(currentMappedGroup) == false)
                    {
                        // It's possbile that group could have been removed by this point due to time taken to do above lookups
                        try
                        {
                            userToGroupMap.TraverseReverseFromNonLeaf(currentMappedGroup, nonLeafvertexAction, leafVertexAction);
                        }
                        catch (NonLeafVertexNotFoundException<TGroup>)
                        {
                        }
                    }
                }
            }

            return returnGroups;
        }

        /// <summary>
        /// Gets the entities that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified user is mapped to.</returns>
        /// <remarks>Putting implementation of GetUserToEntityMappings() in this method allows methods like GetEntitiesAccessibleByUser() to call this method rather than the public virtual GetUserToEntityMappings().  Calling the public virtual version from GetUserToEntityMappings() can result is overridden implementations being called, and hence unexpected behaviour (specifically metrics being logged unnecessarily).</remarks>
        protected IEnumerable<Tuple<String, String>> GetUserToEntityMappingsImplementation(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            Boolean containsUser = userToEntityMap.TryGetValue(user, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
            if (containsUser == true)
            {
                foreach (KeyValuePair<String, ISet<String>> currentEntityType in entitiesAndTypesInMapping)
                {
                    foreach (String currentEntity in currentEntityType.Value)
                    {
                        yield return new Tuple<String, String>(currentEntityType.Key, currentEntity);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the entities that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified group is mapped to.</returns>
        /// <remarks>Putting implementation of GetUserToEntityMappings() in this method allows methods like GetEntitiesAccessibleByUser() to call this method rather than the public virtual GetUserToEntityMappings().  Calling the public virtual version from GetUserToEntityMappings() can result is overridden implementations being called, and hence unexpected behaviour (specifically metrics being logged unnecessarily).</remarks>
        protected IEnumerable<Tuple<String, String>> GetGroupToEntityMappingsImplementation(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            Boolean containsGroup = groupToEntityMap.TryGetValue(group, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
            if (containsGroup == true)
            {
                foreach (KeyValuePair<String, ISet<String>> currentEntityType in entitiesAndTypesInMapping)
                {
                    foreach (String currentEntity in currentEntityType.Value)
                    {
                        yield return new Tuple<String, String>(currentEntityType.Key, currentEntity);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of entities that the specified user is mapped to.</returns>
        /// <remarks>Putting implementation of GetUserToEntityMappings() in this method allows methods like GetEntitiesAccessibleByUser() to call this method rather than the public virtual GetUserToEntityMappings().  Calling the public virtual version from GetUserToEntityMappings() can result is overridden implementations being called, and hence unexpected behaviour (specifically metrics being logged unnecessarily).</remarks>
        protected IEnumerable<String> GetUserToEntityMappingsImplementation(TUser user, String entityType)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            Boolean containsUser = userToEntityMap.TryGetValue(user, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
            if (containsUser == true)
            {
                Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                if (containsEntity == true)
                {
                    foreach (String currentEntity in entitiesInMapping)
                    {
                        yield return currentEntity;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the groups that are mapped to the specified entity.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a group is mapped to the entity via other groups).</param>
        /// <returns>A collection of groups that are mapped to the specified entity.</returns>
        /// <remarks>Putting implementation of GetEntityToGroupMappings() in this method allows methods like GetEntityToUserMappings() to call this method rather than the public virtual GetEntityToGroupMappings().  Calling the public virtual version from GetEntityToGroupMappings() can result is overridden implementations being called, and hence unexpected behaviour (specifically metrics being logged unnecessarily).</remarks>
        protected IEnumerable<TGroup> GetEntityToGroupMappingsImplementation(String entityType, String entity, Boolean includeIndirectMappings)
        {
            // TODO: Implementation is identical to GetApplicationComponentAndAccessLevelToGroupMappingsImplementation() except the call to GetEntityReverseMappings() and exception handling
            //   Could be combined into common method
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (EntityExists(entityType, entity) == false)
                ThrowEntityDoesntExistException(entityType, entity, nameof(entity));

            var returnGroups = new HashSet<TGroup>();
            GetEntityReverseMappings(groupToEntityReverseMap, entityType, entity, returnGroups);
            var visitedGroups = new HashSet<TGroup>();
            if (includeIndirectMappings == true)
            {
                Func<TGroup, Boolean> nonLeafvertexAction = (TGroup currentGroup) =>
                {
                    // TODO: As per comments in GetApplicationComponentAndAccessLevelToGroupMappings() regarding inefficiencies
                    if (visitedGroups.Contains(currentGroup) == false)
                    {
                        returnGroups.Add(currentGroup);
                        visitedGroups.Add(currentGroup);
                    }

                    return true;
                };
                Func<TUser, Boolean> leafVertexAction = (TUser currentUser) => { return true; };
                var directlyMappedGroups = new List<TGroup>(returnGroups);
                foreach (TGroup currentMappedGroup in directlyMappedGroups)
                {
                    if (visitedGroups.Contains(currentMappedGroup) == false)
                    {
                        // It's possbile that group could have been removed by this point due to time taken to do above lookups
                        try
                        {
                            userToGroupMap.TraverseReverseFromNonLeaf(currentMappedGroup, nonLeafvertexAction, leafVertexAction);
                        }
                        catch (NonLeafVertexNotFoundException<TGroup>)
                        {
                        }
                    }
                }
            }

            return returnGroups;
        }

        /// <summary>
        /// Gets the items which are mapped to by application components and access levels in the specified dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the items which are mapped to.</typeparam>
        /// <param name="componentReverseMap">The application component and access level reverse mapping structure to retrieve the mappings from.</param>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="mappedItems">Hashset to store the found mapped items in.</param>
        protected void GetApplicationComponentAndAccessLevelReverseMappings<T>(IDictionary<TComponent, IDictionary<TAccess, ISet<T>>> componentReverseMap, TComponent applicationComponent, TAccess accessLevel, HashSet<T> mappedItems)
        {
            GetTwoLevelMappings(componentReverseMap, applicationComponent, accessLevel, mappedItems);
        }

        /// <summary>
        /// Gets the items which are mapped to by entities in the specified dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the items which are mapped to.</typeparam>
        /// <param name="entityReverseMap">The entity reverse mapping structure to retrieve the mappings from.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="mappedItems">Hashset to store the found mapped items in.</param>
        protected void GetEntityReverseMappings<T>(IDictionary<String, IDictionary<String, ISet<T>>> entityReverseMap, String entityType, String entity, HashSet<T> mappedItems)
        {
            GetTwoLevelMappings(entityReverseMap, entityType, entity, mappedItems);
        }

        /// <summary>
        /// Gets the items which are mapped to by key values in the specified dictionary.
        /// </summary>
        /// <typeparam name="TKey1">The type of the first level key in the mappings.</typeparam>
        /// <typeparam name="TKey2">The type of the second level key in the mappings.</typeparam>
        /// <typeparam name="TValue">The value of the items which are mapped to.</typeparam>
        /// <param name="mappingStructure">The structure to retrieve the mappings from.</param>
        /// <param name="key1Value">The value of the first level key in the mappings to retrieve.</param>
        /// <param name="key2Value">The value of the second level key in the mappings to retrieve.</param>
        /// <param name="mappedItems">Hashset to store the found mapped items in.</param>
        /// <remarks>Provides a common abstraction to search in reverse mapping structures like members 'userToComponentReverseMap', 'userToEntityReverseMap', etc.</remarks>
        protected void GetTwoLevelMappings<TKey1, TKey2, TValue>(IDictionary<TKey1, IDictionary<TKey2, ISet<TValue>>> mappingStructure, TKey1 key1Value, TKey2 key2Value, HashSet<TValue> mappedItems)
        {
            Boolean key1MappingsExist = mappingStructure.TryGetValue(key1Value, out IDictionary<TKey2, ISet<TValue>> key1Mappings);
            if (key1MappingsExist == true)
            {
                Boolean key2MappingsExist = key1Mappings.TryGetValue(key2Value, out ISet<TValue> items);
                if (key2MappingsExist == true)
                {
                    foreach (TValue currentItem in items)
                    {
                        if (mappedItems.Contains(currentItem) == false)
                        {
                            mappedItems.Add(currentItem);
                        }
                    }
                }
            }
        }

        #pragma warning disable 1591

        protected void ThrowUserDoesntExistException(TUser user, String parameterName)
        {
            throw new UserNotFoundException<TUser>($"User '{user.ToString()}' does not exist.", parameterName, user);
        }

        protected void ThrowUserDoesntExistException(TUser user, String parameterName, Exception innerException)
        {
            throw new UserNotFoundException<TUser>($"User '{user.ToString()}' does not exist.", parameterName, user, innerException);
        }

        protected void ThrowGroupDoesntExistException(TGroup group, String parameterName)
        {
            throw new GroupNotFoundException<TGroup>($"Group '{group.ToString()}' does not exist.", parameterName, group);
        }

        protected void ThrowGroupDoesntExistException(TGroup group, String parameterName, Exception innerException)
        {
            throw new GroupNotFoundException<TGroup>($"Group '{group.ToString()}' does not exist.", parameterName, group, innerException);
        }

        protected void ThrowEntityTypeDoesntExistException(String entityType, String parameterName)
        {
            throw new EntityTypeNotFoundException($"Entity type '{entityType}' does not exist.", parameterName, entityType);
        }

        protected void ThrowEntityDoesntExistException(String entityType, String entity, String parameterName)
        {
            throw new EntityNotFoundException($"Entity '{entity}' does not exist.", parameterName, entityType, entity);
        }

        #endregion

        #pragma warning restore 1591
    }
}
