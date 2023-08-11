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
using ApplicationAccess.Utilities;

namespace ApplicationAccess
{
    /// <summary>
    /// A thread-safe version of the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class, which can be accessed and modified by multiple threads concurrently.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Thread safety is implemented by using concurrent collections internally to represent the user, group, component, access level, and entity mappings (allows for concurrent read and enumeration operations), and locks to serialize modification operations.  Note that all generic type parameters must implement relevant methods to allow storing in a <see cref="System.Collections.Generic.HashSet{T}"/> (at minimum <see cref="IEquatable{T}"/> and <see cref="Object.GetHashCode">GetHashcode()</see>).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</remarks>
    public class ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> : AccessManagerBase<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Manages acquiring locks on underlying sets and dictionaries.</summary>
        protected LockManager lockManager;
        /// <summary>Lock object for the users collection of the base class directed graph member.</summary>
        protected Object usersLock;
        /// <summary>Lock object for the groups collection of the base class directed graph member.</summary>
        protected Object groupsLock;
        /// <summary>Lock object for the user to group map of the base class directed graph member.</summary>
        protected Object userToGroupMapLock;
        /// <summary>Lock object for the group to group map of the base class directed graph member.</summary>
        protected Object groupToGroupMapLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentAccessManager class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public ConcurrentAccessManager(Boolean storeBidirectionalMappings)
            : base(new ConcurrentCollectionFactory(), new ConcurrentDirectedGraph<TUser, TGroup>(false, storeBidirectionalMappings), storeBidirectionalMappings)
        {
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentAccessManager class.
        /// </summary>
        /// <param name="concurrentDirectedGraph">The ConcurrentDirectedGraph instance to use to store users and groups.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public ConcurrentAccessManager(ConcurrentDirectedGraph<TUser, TGroup> concurrentDirectedGraph, Boolean storeBidirectionalMappings)
            : base(new ConcurrentCollectionFactory(), concurrentDirectedGraph, storeBidirectionalMappings)
        {
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentAccessManager class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ConcurrentAccessManager(ICollectionFactory collectionFactory, Boolean storeBidirectionalMappings)
            : base(collectionFactory, new ConcurrentDirectedGraph<TUser, TGroup>(collectionFactory, false, storeBidirectionalMappings), storeBidirectionalMappings)
        {
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Removes all items and mappings from the graph.
        /// </summary>
        /// <remarks>Since the Clear() method on HashSets and Dictionaries underlying the class are O(n) operations, performance will scale roughly with the number of items and mappings stored in the access manager.</remarks>
        public override void Clear()
        {
            lockManager.AcquireAllLocksAndInvokeAction(new Action(() =>
            {
                base.Clear();
            }));
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddUser(user, wrappingAction);
        }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the user but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddUser(TUser user, Action<TUser> postProcessingAction)
        {

            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user);
            };
            this.AddUser(user, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveUser(user, wrappingAction);
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the user but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user);
            };
            this.RemoveUser(user, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddGroup(group, wrappingAction);
        }

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the group but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group);
            };
            this.AddGroup(group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveGroup(group, wrappingAction);
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the group but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group);
            };
            this.RemoveGroup(group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddUserToGroupMapping(user, group, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, group);
            };
            this.AddUserToGroupMapping(user, group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveUserToGroupMapping(user, group, wrappingAction);
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, group);
            };
            this.RemoveUserToGroupMapping(user, group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            this.AddGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            this.RemoveGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            };
            this.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            };
            this.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            };
            this.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            };
            this.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddEntityType(entityType, wrappingAction);
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(entityType);
            };
            this.AddEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveEntityType(entityType, wrappingAction);
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(entityType);
            };
            this.RemoveEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddEntity(entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(entityType, entity);
            };
            this.AddEntity(entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveEntity(entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(entityType, entity);
            };
            this.RemoveEntity(entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, entityType, entity);
            };
            this.AddUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(user, entityType, entity);
            };
            this.RemoveUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group, entityType, entity);
            };
            this.AddGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public virtual void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                baseAction.Invoke();
                postProcessingAction.Invoke(group, entityType, entity);
            };
            this.RemoveGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the classes' lock objects and dependencies.
        /// </summary>
        protected void InitializeLockObjects()
        {
            usersLock = new Object();
            groupsLock = new Object();
            userToGroupMapLock = new Object();
            groupToGroupMapLock = new Object();
            lockManager.RegisterLockObject(usersLock);
            lockManager.RegisterLockObject(groupsLock);
            lockManager.RegisterLockObject(userToGroupMapLock);
            lockManager.RegisterLockObject(groupToGroupMapLock);
            lockManager.RegisterLockObject(userToComponentMap);
            lockManager.RegisterLockObject(groupToComponentMap);
            lockManager.RegisterLockObject(entities);
            lockManager.RegisterLockObject(userToEntityMap);
            lockManager.RegisterLockObject(groupToEntityMap);
            lockManager.RegisterLockObjectDependency(userToGroupMapLock, usersLock);
            lockManager.RegisterLockObjectDependency(userToGroupMapLock, groupsLock);
            lockManager.RegisterLockObjectDependency(groupToGroupMapLock, groupsLock);
            lockManager.RegisterLockObjectDependency(userToComponentMap, usersLock);
            lockManager.RegisterLockObjectDependency(userToEntityMap, usersLock);
            lockManager.RegisterLockObjectDependency(userToEntityMap, entities);
            lockManager.RegisterLockObjectDependency(groupToComponentMap, groupsLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, groupsLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, entities);
        }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the user, allowing arbitrary code to be run before and/or after adding the user, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the user being added, and the action which actually adds the user.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the user.</remarks>
        protected void AddUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(usersLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(user, () => { base.AddUser(user); });
            }));
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the user, allowing arbitrary code to be run before and/or after removing the user, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the user being removed, and the action which actually removes the user.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the user.</remarks>
        protected void RemoveUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(usersLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(user, () => { base.RemoveUser(user); });
            }));
        }

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the group, allowing arbitrary code to be run before and/or after adding the group, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the group being added, and the action which actually adds the group.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the group.</remarks>
        protected void AddGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupsLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(group, () => { base.AddGroup(group); });
            }));
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the group, allowing arbitrary code to be run before and/or after removing the group, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the group being removed, and the action which actually removes the group.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the group.</remarks>
        protected void RemoveGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupsLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(group, () => { base.RemoveGroup(group); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the user in the mapping, the group in the mapping, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>
        protected void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(user, group, () => { base.AddUserToGroupMapping(user, group); });
            }));
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the user in the mapping, the group in the mapping, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(user, group, () => { base.RemoveUserToGroupMapping(user, group); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' group in the mapping, the 'to' group in the mapping, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>
        protected void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(fromGroup, toGroup, () => { base.AddGroupToGroupMapping(fromGroup, toGroup); });
            }));
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the 'from' group in the mapping, the 'to' group in the mapping, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(fromGroup, toGroup, () => { base.RemoveGroupToGroupMapping(fromGroup, toGroup); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the user in the mapping, the application component in the mapping, the level of access to the component, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>
        protected void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(user, applicationComponent, accessLevel, () => { base.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel); });
            }));
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the user in the mapping, the application component in the mapping, the level of access to the component, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(user, applicationComponent, accessLevel, () => { base.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the group in the mapping, the application component in the mapping, the level of access to the component, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>
        protected void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(group, applicationComponent, accessLevel, () => { base.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel); });
            }));
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the group in the mapping, the application component in the mapping, the level of access to the component, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(group, applicationComponent, accessLevel, () => { base.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel); });
            }));
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the entity type, allowing arbitrary code to be run before and/or after adding the entity type, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the entity type being added, and the action which actually adds the entity type.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the entity type.</remarks>
        protected void AddEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(entityType, () => { base.AddEntityType(entityType); });
            }));
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the entity type, allowing arbitrary code to be run before and/or after removing the entity type, but whilst any mutual-exclusion locks are still acquired.  Accepts 2 parameters: the entity type being removed, and the action which actually removes the entity type.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the entity type.</remarks>
        protected void RemoveEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(entityType, () => { base.RemoveEntityType(entityType); });
            }));
        }

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the entity, allowing arbitrary code to be run before and/or after adding the entity, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the type of the entity, the entity being added, and the action which actually adds the entity.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the entity.</remarks>
        protected void AddEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(entityType, entity, () => { base.AddEntity(entityType, entity); });
            }));
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the entity, allowing arbitrary code to be run before and/or after removing the entity, but whilst any mutual-exclusion locks are still acquired.  Accepts 3 parameters: the type of the entity, the entity being removed, and the action which actually removes the entity.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the entity.</remarks>
        protected void RemoveEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(entityType, entity, () => { base.RemoveEntity(entityType, entity); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the user in the mapping, the type of the entity, the entity in the mapping, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>
        protected void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(user, entityType, entity, () => { base.AddUserToEntityMapping(user, entityType, entity); });
            }));
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the user in the mapping, the type of the entity, the entity in the mapping, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(user, entityType, entity, () => { base.RemoveUserToEntityMapping(user, entityType, entity); });
            }));
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to add the mapping, allowing arbitrary code to be run before and/or after adding the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the group in the mapping, the type of the entity, the entity in the mapping, and the action which actually adds the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to add the mapping.</remarks>

        protected void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                wrappingAction.Invoke(group, entityType, entity, () => { base.AddGroupToEntityMapping(group, entityType, entity); });
            }));
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">An action which wraps the operation to remove the mapping, allowing arbitrary code to be run before and/or after removing the mapping, but whilst any mutual-exclusion locks are still acquired.  Accepts 4 parameters: the group in the mapping, the type of the entity, the entity in the mapping, and the action which actually removes the mapping.</param>
        /// <remarks>Parameter 'wrappingAction' inner Action parameter must be invoked during the invocation of the outer 'wrappingAction' in order to remove the mapping.</remarks>
        protected void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() =>
            {
                wrappingAction.Invoke(group, entityType, entity, () => { base.RemoveGroupToEntityMapping(group, entityType, entity); });
            }));
        }

        #endregion
    }
}
