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
using System.Linq;

namespace ApplicationAccess
{
    /// <summary>
    /// A base class for managing the access of users and groups of users to components and entities within an application.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Note that all generic type parameters must implement relevant methods to allow storing in a HashSet (at minimum IEquatable&lt;T&gt; and GetHashcode()).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</remarks>
    public abstract class AccessManagerBase<TUser, TGroup, TComponent, TAccess> : IAccessManager<TUser, TGroup, TComponent, TAccess>
    {        
        /// <summary>Creates instances of collection classes.</summary>
        protected ICollectionFactory collectionFactory;
        /// <summary>The DirectedGraph which stores the user to group mappings.</summary>
        protected DirectedGraphBase<TUser, TGroup> userToGroupMap;
        /// <summary>A dictionary which stores mappings between a user, and application component, and a level of access to that component.</summary>
        protected IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel>> userToComponentMap;
        /// <summary>A dictionary which HashSet mappings between a group, and application component, and a level of access to that component.</summary>
        protected IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel>> groupToComponentMap;
        /// <summary>Holds all valid entity types and values within the access manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).</summary>
        protected IDictionary<String, ISet<String>> entities;
        /// <summary>A dictionary which stores user to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the user.</summary>
        protected IDictionary<TUser, IDictionary<String, ISet<String>>> userToEntityMap;
        /// <summary>A dictionary which stores group to entity mappings.  The value stores another dictionary whose key contains the entity type and whose value contains the name of all entities of the specified type which are mapped to the group.</summary>
        protected IDictionary<TGroup, IDictionary<String, ISet<String>>> groupToEntityMap;

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        public IEnumerable<TUser> Users
        {
            get
            {
                return userToGroupMap.LeafVertices;
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                return userToGroupMap.NonLeafVertices;
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
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
        /// <param name="collectionFactory">>Creates instances of collection classes.</param>
        /// <param name="userToGroupMap">The user to group map the access manager should use internally.</param>
        public AccessManagerBase(ICollectionFactory collectionFactory, DirectedGraphBase<TUser, TGroup> userToGroupMap)
        {
            this.collectionFactory = collectionFactory;
            this.userToGroupMap = userToGroupMap;
            userToComponentMap = this.collectionFactory.GetDictionaryInstance<TUser, ISet<ApplicationComponentAndAccessLevel>>();
            groupToComponentMap = this.collectionFactory.GetDictionaryInstance<TGroup, ISet<ApplicationComponentAndAccessLevel>>();
            entities = this.collectionFactory.GetDictionaryInstance<String, ISet<String>>();
            userToEntityMap = this.collectionFactory.GetDictionaryInstance<TUser, IDictionary<String, ISet<String>>>();
            groupToEntityMap = this.collectionFactory.GetDictionaryInstance<TGroup, IDictionary<String, ISet<String>>>();
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        public Boolean ContainsUser(TUser user)
        {
            return userToGroupMap.ContainsLeafVertex(user);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
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
                userToComponentMap.Remove(user);
            }
            if (userToEntityMap.ContainsKey(user) == true)
            {
                userToEntityMap.Remove(user);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        public Boolean ContainsGroup(TGroup group)
        {
            return userToGroupMap.ContainsNonLeafVertex(group);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
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
                groupToComponentMap.Remove(group);
            }
            if (groupToEntityMap.ContainsKey(group) == true)
            {
                groupToEntityMap.Remove(group);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            try
            {
                return userToGroupMap.GetLeafEdges(user);
            }
            catch (LeafVertexNotFoundException<TUser> e)
            {
                ThrowUserDoesntExistException(user, nameof(user), e);

                return Enumerable.Empty<TGroup>();
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public virtual void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            if (userToGroupMap.ContainsNonLeafVertex(fromGroup) == false)
                throw new ArgumentException($"Group '{fromGroup.ToString()}' does not exist.", nameof(fromGroup));
            if (userToGroupMap.ContainsNonLeafVertex(toGroup) == false)
                throw new ArgumentException($"Group '{toGroup.ToString()}' does not exist.", nameof(toGroup));
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            try
            {
                return userToGroupMap.GetNonLeafEdges(group);
            }
            catch (NonLeafVertexNotFoundException<TGroup> e)
            {
                ThrowGroupDoesntExistException(group, nameof(group), e);

                return Enumerable.Empty<TGroup>();
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public virtual void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            if (userToGroupMap.ContainsNonLeafVertex(fromGroup) == false)
                throw new ArgumentException($"Group '{fromGroup.ToString()}' does not exist.", nameof(fromGroup));
            if (userToGroupMap.ContainsNonLeafVertex(toGroup) == false)
                throw new ArgumentException($"Group '{toGroup.ToString()}' does not exist.", nameof(toGroup));

            try
            {
                userToGroupMap.RemoveNonLeafToNonLeafEdge(fromGroup, toGroup);
            }
            catch (NonLeafToNonLeafEdgeNotFoundException<TGroup> nonLeafToNonLeafEdgeNotFoundException)
            {
                throw new ArgumentException($"A mapping between groups '{fromGroup.ToString()}' and '{toGroup.ToString()}' does not exist.", nameof(toGroup), nonLeafToNonLeafEdgeNotFoundException);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public virtual void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var componentAndAccess = new ApplicationComponentAndAccessLevel(applicationComponent, accessLevel);
            if (userToComponentMap.ContainsKey(user) == true)
            {
                if (userToComponentMap[user].Contains(componentAndAccess) == true)
                    throw new ArgumentException($"A mapping between user '{user.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' already exists.");
            }
            else
            {
                userToComponentMap.Add(user, collectionFactory.GetSetInstance<ApplicationComponentAndAccessLevel>());
            }
            userToComponentMap[user].Add(componentAndAccess);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            if (userToComponentMap.ContainsKey(user) == true)
            {
                foreach (ApplicationComponentAndAccessLevel currentPair in userToComponentMap[user])
                {
                    yield return new Tuple<TComponent, TAccess>(currentPair.ApplicationComponent, currentPair.AccessLevel);
                }
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public virtual void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            var componentAndAccess = new ApplicationComponentAndAccessLevel(applicationComponent, accessLevel);
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
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public virtual void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var componentAndAccess = new ApplicationComponentAndAccessLevel(applicationComponent, accessLevel);
            if (groupToComponentMap.ContainsKey(group) == true)
            {
                if (groupToComponentMap[group].Contains(componentAndAccess) == true)
                    throw new ArgumentException($"A mapping between group '{group.ToString()}' application component '{applicationComponent.ToString()}' and access level '{accessLevel.ToString()}' already exists.");
            }
            else
            {
                groupToComponentMap.Add(group, collectionFactory.GetSetInstance<ApplicationComponentAndAccessLevel>());
            }
            groupToComponentMap[group].Add(componentAndAccess);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            if (groupToComponentMap.ContainsKey(group) == true)
            {
                foreach (ApplicationComponentAndAccessLevel currentPair in groupToComponentMap[group])
                {
                    yield return new Tuple<TComponent, TAccess>(currentPair.ApplicationComponent, currentPair.AccessLevel);
                }
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public virtual void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            var componentAndAccess = new ApplicationComponentAndAccessLevel(applicationComponent, accessLevel);
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
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public virtual void AddEntityType(String entityType)
        {
            if (entities.ContainsKey(entityType) == true)
                throw new ArgumentException($"Entity type '{entityType}' already exists.", nameof(entityType));
            if (String.IsNullOrWhiteSpace(entityType) == true)
                throw new ArgumentException($"Entity type '{entityType}' must contain a valid character.", nameof(entityType));

            entities.Add(entityType, collectionFactory.GetSetInstance<String>());
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        public Boolean ContainsEntityType(String entityType)
        {
            return entities.ContainsKey(entityType);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public virtual void RemoveEntityType(String entityType)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            foreach (KeyValuePair<TUser, IDictionary<String, ISet<String>>> currentKvp in userToEntityMap)
            {
                if (currentKvp.Value.ContainsKey(entityType) == true)
                {
                    currentKvp.Value.Remove(entityType);
                }
            }
            foreach (KeyValuePair<TGroup, IDictionary<String, ISet<String>>> currentKvp in groupToEntityMap)
            {
                if (currentKvp.Value.ContainsKey(entityType) == true)
                {
                    currentKvp.Value.Remove(entityType);
                }
            }
            entities.Remove(entityType);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
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

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            return entities[entityType];
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return (entities.ContainsKey(entityType) && entities[entityType].Contains(entity));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public virtual void RemoveEntity(String entityType, String entity)
        {
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));

            foreach (KeyValuePair<TUser, IDictionary<String, ISet<String>>> currentKvp in userToEntityMap)
            {
                if (currentKvp.Value.ContainsKey(entityType) == true && currentKvp.Value[entityType].Contains(entity) == true)
                {
                    currentKvp.Value[entityType].Remove(entity);
                }
            }
            foreach (KeyValuePair<TGroup, IDictionary<String, ISet<String>>> currentKvp in groupToEntityMap)
            {
                if (currentKvp.Value.ContainsKey(entityType) == true && currentKvp.Value[entityType].Contains(entity) == true)
                {
                    currentKvp.Value[entityType].Remove(entity);
                }
            }
            entities[entityType].Remove(entity);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public virtual void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));
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
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));

            if (userToEntityMap.ContainsKey(user) == true)
            {
                foreach (KeyValuePair<String, ISet<String>> currentEntityType in userToEntityMap[user])
                {
                    foreach (String currentEntity in currentEntityType.Value)
                    {
                        yield return new Tuple<String, String>(currentEntityType.Key, currentEntity);
                    }
                }
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true)
            {
                return userToEntityMap[user][entityType];
            }
            else
            {
                return Enumerable.Empty<String>();
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public virtual void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));
            if (userToEntityMap.ContainsKey(user) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true && userToEntityMap[user][entityType].Contains(entity) == false)
                throw new ArgumentException($"A mapping between user '{user.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");

            userToEntityMap[user][entityType].Remove(entity);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public virtual void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));
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
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));

            if (groupToEntityMap.ContainsKey(group) == true)
            {
                foreach (KeyValuePair<String, ISet<String>> currentEntityType in groupToEntityMap[group])
                {
                    foreach (String currentEntity in currentEntityType.Value)
                    {
                        yield return new Tuple<String, String>(currentEntityType.Key, currentEntity);
                    }
                }
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == true)
            {
                return groupToEntityMap[group][entityType];
            }
            else
            {
                return Enumerable.Empty<String>();
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public virtual void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
                ThrowGroupDoesntExistException(group, nameof(group));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));
            if (groupToEntityMap.ContainsKey(group) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");
            if (groupToEntityMap.ContainsKey(group) == true && groupToEntityMap[group].ContainsKey(entityType) == true && groupToEntityMap[group][entityType].Contains(entity) == false)
                throw new ArgumentException($"A mapping between group '{group.ToString()}' and entity '{entity}' with type '{entityType}' doesn't exist.");

            groupToEntityMap[group][entityType].Remove(entity);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToApplicationComponent(`0,`2,`3)"]/*'/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                return false;
            }
            var componentAndAccess = new ApplicationComponentAndAccessLevel(applicationComponent, accessLevel);
            if (userToComponentMap.ContainsKey(user) == true && userToComponentMap[user].Contains(componentAndAccess) == true)
            {
                return true;
            }
            Boolean hasAccess = false;
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                if (groupToComponentMap.ContainsKey(currentGroup) == true && groupToComponentMap[currentGroup].Contains(componentAndAccess) == true)
                {
                    hasAccess = true;
                    return false;
                }
                else
                {
                    return true;
                }
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return hasAccess;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccessToEntity(`0,System.String,System.String)"]/*'/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                return false;
            }
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));
            if (entities[entityType].Contains(entity) == false)
                ThrowEntityDoesntExistException(entity, nameof(entity));
            if (userToEntityMap.ContainsKey(user) == true && userToEntityMap[user].ContainsKey(entityType) == true && userToEntityMap[user][entityType].Contains(entity) == true)
            {
                return true;
            }
            Boolean hasAccess = false;
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                if (groupToEntityMap.ContainsKey(currentGroup) == true && groupToEntityMap[currentGroup].ContainsKey(entityType) == true && groupToEntityMap[currentGroup][entityType].Contains(entity) == true)
                {
                    hasAccess = true;
                    return false;
                }
                else
                {
                    return true;
                }
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return hasAccess;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetAccessibleEntities(`0,System.String)"]/*'/>
        public HashSet<String> GetAccessibleEntities(TUser user, String entityType)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
                ThrowUserDoesntExistException(user, nameof(user));
            if (entities.ContainsKey(entityType) == false)
                ThrowEntityTypeDoesntExistException(entityType, nameof(entityType));

            var returnEntities = new HashSet<String>();
            foreach (String currentEntity in GetUserToEntityMappings(user, entityType))
            {
                returnEntities.Add(currentEntity);
            }
            Func<TGroup, Boolean> vertexAction = (TGroup currentGroup) =>
            {
                if (groupToEntityMap.ContainsKey(currentGroup) == true && groupToEntityMap[currentGroup].ContainsKey(entityType) == true)
                {
                    returnEntities.UnionWith(groupToEntityMap[currentGroup][entityType]);
                }

                return true;
            };
            userToGroupMap.TraverseFromLeaf(user, vertexAction);

            return returnEntities;
        }

        #pragma warning disable 1591

        #region Private/Protected Methods

        protected void ThrowUserDoesntExistException(TUser user, String parameterName)
        {
            throw new ArgumentException($"User '{user.ToString()}' does not exist.", parameterName);
        }

        protected void ThrowUserDoesntExistException(TUser user, String parameterName, Exception innerException)
        {
            throw new ArgumentException($"User '{user.ToString()}' does not exist.", parameterName, innerException);
        }

        protected void ThrowGroupDoesntExistException(TGroup group, String parameterName)
        {
            throw new ArgumentException($"Group '{group.ToString()}' does not exist.", parameterName);
        }

        protected void ThrowGroupDoesntExistException(TGroup group, String parameterName, Exception innerException)
        {
            throw new ArgumentException($"Group '{group.ToString()}' does not exist.", parameterName, innerException);
        }

        protected void ThrowEntityTypeDoesntExistException(String entityType, String parameterName)
        {
            throw new ArgumentException($"Entity type '{entityType}' does not exist.", parameterName);
        }

        protected void ThrowEntityDoesntExistException(String entity, String parameterName)
        {
            throw new ArgumentException($"Entity '{entity}' does not exist.", parameterName);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Container class which holds an application component and a level of access of that component.
        /// </summary>
        protected class ApplicationComponentAndAccessLevel : IEquatable<ApplicationComponentAndAccessLevel>
        {
            protected static readonly Int32 prime1 = 7;
            protected static readonly Int32 prime2 = 11;

            protected TComponent applicationComponent;
            protected TAccess accessLevel;

            /// <summary>
            /// The application component.
            /// </summary>
            public TComponent ApplicationComponent
            {
                get { return applicationComponent; }
            }

            /// <summary>
            /// The level of access.
            /// </summary>
            public TAccess AccessLevel
            {
                get { return accessLevel; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.AccessManager+ApplicationComponentAndAccessLevel class.
            /// </summary>
            /// <param name="applicationComponent">The application component.</param>
            /// <param name="accessLevel">The level of access.</param>
            public ApplicationComponentAndAccessLevel(TComponent applicationComponent, TAccess accessLevel)
            {
                this.applicationComponent = applicationComponent;
                this.accessLevel = accessLevel;
            }

            public Boolean Equals(ApplicationComponentAndAccessLevel other)
            {
                return (this.applicationComponent.Equals(other.applicationComponent) && this.accessLevel.Equals(other.accessLevel));
            }

            public override Int32 GetHashCode()
            {
                return (prime1 * applicationComponent.GetHashCode() + prime2 * accessLevel.GetHashCode());
            }
        }

        #endregion

        #pragma warning restore 1591
    }
}
