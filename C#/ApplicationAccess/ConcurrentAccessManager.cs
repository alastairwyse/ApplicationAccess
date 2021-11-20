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
    /// A thread-safe version of the AccessManager class, which can be accessed and modified by multiple threads concurrently.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Thread safety is implemented by using concurrent collections internally to represent the user, group, component, access level, and entity mappings (allows for concurrent read and enumeration operations), and locks to serialize modification operations.  Note that all generic type parameters must implement relevant methods to allow storing in a HashSet (at minimum IEquatable&lt;T&gt; and GetHashcode()).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</remarks>
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
        public ConcurrentAccessManager()
            : base(new ConcurrentCollectionFactory(), new ConcurrentDirectedGraph<TUser, TGroup>(false))
        {
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.ConcurrentAccessManager class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public ConcurrentAccessManager(ICollectionFactory collectionFactory)
            : base(collectionFactory, new ConcurrentDirectedGraph<TUser, TGroup>(collectionFactory, false))
        {
            lockManager = new LockManager();
            InitializeLockObjects();
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public override void AddUser(TUser user)
        {
            AddUser(user, new Action<TUser>((actionUser) => { }));
        }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the user but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUser(TUser user, Action<TUser> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(usersLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddUser(user);
                postProcessingAction.Invoke(user);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public override void RemoveUser(TUser user)
        {
            RemoveUser(user, new Action<TUser>((actionUser) => { }));
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the user but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(usersLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            {
                base.RemoveUser(user);
                postProcessingAction.Invoke(user);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public override void AddGroup(TGroup group)
        {
            AddGroup(group, new Action<TGroup>((actionGroup) => { }));
        }

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the group but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupsLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddGroup(group);
                postProcessingAction.Invoke(group);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public override void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, new Action<TGroup>((actionGroup) => { }));
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the group but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupsLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveGroup(group);
                postProcessingAction.Invoke(group);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, new Action<TUser, TGroup>((actionUser, actionGroup) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddUserToGroupMapping(user, group);
                postProcessingAction.Invoke(user, group);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, new Action<TUser, TGroup>((actionUser, actionGroup) => { }));
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveUserToGroupMapping(user, group);
                postProcessingAction.Invoke(user, group);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, new Action<TGroup, TGroup>((actionFromGroup, actionToGroup) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddGroupToGroupMapping(fromGroup, toGroup);
                postProcessingAction.Invoke(fromGroup, toGroup);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToGroupMapping(`1,`1)"]/*'/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, new Action<TGroup, TGroup>((actionFromGroup, actionToGroup) => { }));
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToGroupMapLock, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveGroupToGroupMapping(fromGroup, toGroup);
                postProcessingAction.Invoke(fromGroup, toGroup);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, new Action<TUser, TComponent, TAccess>((actionUser, actionApplicationComponent, actionAccessLevel) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, new Action<TUser, TComponent, TAccess>((actionUser, actionApplicationComponent, actionAccessLevel) => { }));
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, new Action<TGroup, TComponent, TAccess>((actionGroup, actionApplicationComponent, actionAccessLevel) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, new Action<TGroup, TComponent, TAccess>((actionGroup, actionApplicationComponent, actionAccessLevel) => { }));
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToComponentMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public override void AddEntityType(String entityType)
        {
            AddEntityType(entityType, new Action<String>((actionEntityType) => { }));
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddEntityType(String entityType, Action<String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddEntityType(entityType);
                postProcessingAction.Invoke(entityType);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntityType(System.String)"]/*'/>
        public override void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, new Action<String>((actionEntityType) => { }));
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveEntityType(String entityType, Action<String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveEntityType(entityType);
                postProcessingAction.Invoke(entityType);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntity(System.String,System.String)"]/*'/>
        public override void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, new Action<String, String>((actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddEntity(entityType, entity);
                postProcessingAction.Invoke(entityType, entity);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveEntity(System.String,System.String)"]/*'/>
        public override void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, new Action<String, String>((actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(entities, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveEntity(entityType, entity);
                postProcessingAction.Invoke(entityType, entity);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, new Action<TUser, String, String>((actionUser, actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddUserToEntityMapping(user, entityType, entity);
                postProcessingAction.Invoke(user, entityType, entity);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToEntityMapping(`0,System.String,System.String)"]/*'/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, new Action<TUser, String, String>((actionUser, actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(userToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveUserToEntityMapping(user, entityType, entity);
                postProcessingAction.Invoke(user, entityType, entity);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, new Action<TGroup, String, String>((actionGroup, actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() => 
            { 
                base.AddGroupToEntityMapping(group, entityType, entity);
                postProcessingAction.Invoke(group, entityType, entity);
            }));
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToEntityMapping(`1,System.String,System.String)"]/*'/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, new Action<TGroup, String, String>((actionGroup, actionEntityType, actionEntity) => { }));
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            lockManager.AcquireLocksAndInvokeAction(groupToEntityMap, LockObjectDependencyPattern.ObjectAndObjectsWhichAreDependentOnIt, new Action(() => 
            { 
                base.RemoveGroupToEntityMapping(group, entityType, entity);
                postProcessingAction.Invoke(group, entityType, entity);
            }));
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
            lockManager.RegisterLockObjectDependency(groupToComponentMap, groupsLock);
            lockManager.RegisterLockObjectDependency(userToEntityMap, entities);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, groupsLock);
            lockManager.RegisterLockObjectDependency(groupToEntityMap, entities);
        }

        #endregion
    }
}
