/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess
{
    /// <summary>
    /// Subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> where...
    /// <para>1. Event methods can be called successfully without first satisfying data element dependecies, e.g. the AddUserToGroupMapping() method can be used to add a user to group mapping, without first explicitly adding the user and group.</para>
    /// <para>2. Event methods are idempotent, e.g. the AddUserToGroupMapping() method will return success if the specified mapping already exists.</para>
    /// <para>3. The class passes any depended-on/prepended events to an instance of <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/>, e.g. if the AddUserToGroupMapping() method is called and the specified user and group do not exist, events will be passed to the <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> instance to add the user and group (in addition to them being added to the class internally).</para>
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /*
         * -- General Explanation of Implementation Techniques Used in This Class --
         * 
         * Techniques used can generally be split into 4 categories, and the techniques are repeated for all methods that fall into a given category...
         *   1) Primary Add Methods (e.g. AddUser())
         *     We need to have different behaviour depending on whether the method is begin called directly, or being called as part of a prepended event of a secondary add method (like AddUserToGroupMapping()).
         *     Because if it's being called as a prepended event, we want to emit an event to the 'eventProcessor' member (but we don't if it's being called directly).
         *     Hence we create a protected overload of the method with a Boolean 'generateEvent' parameter... the public version of the method (direct call) will set that parameter to false.
         *     The same protected overload is called from secondary add methods, but the 'generateEvent' parameter is set true
         *     The protected overload itself does NOT call the 'baseAction' action to reuse the same method in the base classes because the base implementations (usually in AccessManagerBase) throw exceptions if the element already exists.
         *     We could catch that exception in this class and ignore, but that would have a performance impact, hence decided to call into the base structures to add directly (first checking that it doesn't already exist)
         *   2) Primary Remove Methods (e.g. RemoveUser())
         *     We don't need to worry about prepended events like with primary add methods.
         *     In this case we MUST call the 'baseAction' action to reuse the same method in the base classes, since those base class methods do cleanup of any dependent elements (e.g. cleanup user to group mappings when a user is removed)
         *     Base class methods since not idempotent will throw an error if the element already exists, so we just check whether the element exists before invoking 'baseAction'.
         *     Hence if the element doesn't exist the method does nothing except call the postProcessingAction (if it was passed)
         *   3) Secondary Add Methods (e.g. AddUserToGroupMapping())
         *     These emit any required prepended events by calling the aforementioned primary add methods with 'generateEvent' parameter set to true.
         *     Then the element is added by either of the following methods...
         *       a. In some cases we can't efficiently figure out whether the element already exists... e.g. in the case of user to group mappings, the 'userToGroupMap' member which stores them only lets you iterate all groups mapped to a specified user
         *          Hence in these cases we have to call the 'baseAction' action (which in the above case calls the relevant Add() method on the userToGroupMap) AND swallow any exceptions that result from the element already existing
         *          Swallowing exceptions is not the preferred approach (due to performance impact), but it's better than iterating a list to check if something exists already
         *       b. In other cases we can efficiently check whether an element already exists... e.g. for AddUserToApplicationComponentAndAccessLevelMapping, we can just check the 'userToComponentMap' member
         *          So in these cases we just check and call the 'baseAction' if it doesn't already exist (knowing that no exception handlers in the base class methods will be triggered since all dependencies will already be met)
         *   4) Secondary Remove Methods (e.g. RemoveUserToGroupMapping())
         *     We don't need to worry about any prepended (or appended) events
         *     Case is similar to secondary add methods above... depending on whether we can efficiently check for existence, we either invoke 'baseAction' and swallow any exceptions, or check existence and call 'baseAction' depending on the result
         *     
         * General techniques
         *   1) Generally we override 2x methods from ConcurrentAccessManager for each type of operation
         *     a. The public method that's part of IAccessManager... like AddUser(TUser user)
         *     b. The overload which takes the 'postProcessingAction' parameter
         *     We have to override the IAccessManager overload because in ConcurrentAccessManager it invokes 'baseAction' by default, any we need to either not call 'baseAction' (e.g. in the case of primary add methods), or only call it conditionally to implement idempotency
         *     We overide the 'postProcessingAction' overload with the actual implementation for this class... either bypassing 'baseAction' or conditionally calling 'baseAction' as described above (and then invoking the 'postProcessingAction')
         *   2) Try to always invoke the 'baseAction' action to implement the actual underlying work... better to reuse base class functionality as much as possible (primary add methods are a case where we cannot invoke 'baseAction' as described above)
         *     This is particularly important for primary reomve methods where the base class methods do cleanup of any dependent elements
         */

        /// <summary>The event processor to pass any depended-on/prepended events to.</summary>
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessor;

        /// <summary>
        /// The event processor to pass any depended-on/prepended events to.
        /// </summary>
        public IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> EventProcessor
        {
            // TODO: Ideally would like to put this in the constructor, BUT it has a circular dependency relationship with DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer
            //   hence, one of them has to be set on the other via a setter.  Don't really like this as setter could be set again mid-use (constructor is much cleaner), but don't
            //   think I have any other choice.
            set
            {
                eventProcessor = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeAccessManager(Boolean storeBidirectionalMappings)
            : base(storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="concurrentDirectedGraph">The ConcurrentDirectedGraph instance to use to store users and groups.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeAccessManager(ConcurrentDirectedGraph<TUser, TGroup> concurrentDirectedGraph, Boolean storeBidirectionalMappings)
            : base(concurrentDirectedGraph, storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public DependencyFreeAccessManager(ICollectionFactory collectionFactory, Boolean storeBidirectionalMappings)
            : base(collectionFactory, storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user)
        {
            Action<TUser> postProcessingAction = (actionUser) => { };
            AddUser(user, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                AddUser(user, false);
                postProcessingAction.Invoke(user);
            };
            AddUser(user, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user)
        {
            Action<TUser> postProcessingAction = (actionUser) => { };
            RemoveUser(user, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                if (userToGroupMap.ContainsLeafVertex(user) == true)
                {
                    baseAction.Invoke();
                }
                postProcessingAction.Invoke(user);
            };
            RemoveUser(user, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group)
        {
            Action<TGroup> postProcessingAction = (actionGroup) => { };
            AddGroup(group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                AddGroup(group, false);
                postProcessingAction.Invoke(group);
            };
            AddGroup(group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group)
        {
            Action<TGroup> postProcessingAction = (actionGroup) => { };
            RemoveGroup(group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                if (userToGroupMap.ContainsNonLeafVertex(group) == true)
                {
                    baseAction.Invoke();
                }
                postProcessingAction.Invoke(group);
            };
            RemoveGroup(group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postProcessingAction = (actionUser, actionGroup) => { };
            AddUserToGroupMapping(user, group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                AddUser(user, true);
                AddGroup(group, true);
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, group);
            };
            AddUserToGroupMapping(user, group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup> postProcessingAction = (actionUser, actionGroup) => { };
            RemoveUserToGroupMapping(user, group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action> wrappingAction = (actionUser, actionGroup, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, group);
            };
            RemoveUserToGroupMapping(user, group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postProcessingAction = (actionFromGroup, actionToGroup) => { };
            AddGroupToGroupMapping(fromGroup, toGroup, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            if (fromGroup.Equals(toGroup) == true)
                throw new ArgumentException($"Parameters '{nameof(fromGroup)}' and '{nameof(toGroup)}' cannot contain the same group.", nameof(toGroup));

            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                AddGroup(fromGroup, true);
                AddGroup(toGroup, true);
                try
                {
                    // Would prefer to call baseAction.Invoke() here, but it doesn't allow distinguishing between NonLeafToNonLeafEdgeAlreadyExistsException (which should be swallowed to
                    //   allow idempotency) and CircularReferenceException (which should be rethrown as it's an error case)
                    userToGroupMap.AddNonLeafToNonLeafEdge(fromGroup, toGroup);
                }
                catch (NonLeafToNonLeafEdgeAlreadyExistsException<TGroup>)
                {
                }
                catch (CircularReferenceException circularReferenceException)
                {
                    throw new ArgumentException($"A mapping between groups '{fromGroup.ToString()}' and '{toGroup.ToString()}' cannot be created as it would cause a circular reference.", nameof(toGroup), circularReferenceException);
                }
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            AddGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup> postProcessingAction = (actionFromGroup, actionToGroup) => { };
            RemoveGroupToGroupMapping(fromGroup, toGroup, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action> wrappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            RemoveGroupToGroupMapping(fromGroup, toGroup, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postProcessingAction = (actionUser, actionApplicationComponent, actionAccessLevel) => { };
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                AddUser(user, true);
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            };
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess> postProcessingAction = (actionUser, actionApplicationComponent, actionAccessLevel) => { };
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, applicationComponent, accessLevel);
            };
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postProcessingAction = (actionGroup, actionApplicationComponent, actionAccessLevel) => { };
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                AddGroup(group, true);
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            };
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess> postProcessingAction = (actionGroup, actionApplicationComponent, actionAccessLevel) => { };
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(group, applicationComponent, accessLevel);
            };
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType)
        {
            Action<String> postProcessingAction = (actionEntityType) => { };
            AddEntityType(entityType, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                AddEntityType(entityType, false);
                postProcessingAction.Invoke(entityType);
            };
            AddEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType)
        {
            Action<String> postProcessingAction = (actionEntityType) => { };
            RemoveEntityType(entityType, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                if (entities.ContainsKey(entityType) == true)
                {
                    baseAction.Invoke();
                }
                postProcessingAction.Invoke(entityType);
            };
            RemoveEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity)
        {
            Action<String, String> postProcessingAction = (actionEntityType, actionEntity) => { };
            AddEntity(entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                AddEntityType(entityType, true);
                AddEntity(entityType, entity, false);
                postProcessingAction.Invoke(entityType, entity);
            };
            AddEntity(entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity)
        {
            Action<String, String> postProcessingAction = (actionEntityType, actionEntity) => { };
            RemoveEntity(entityType, entityType, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                if (entities.ContainsKey(entityType) == true && entities[entityType].Contains(entity) == true)
                {
                    baseAction.Invoke();
                }
                postProcessingAction.Invoke(entityType, entity);
            };
            RemoveEntity(entityType, entity, wrappingAction);
        }
        
        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Action<TUser, String, String> postProcessingAction = (actionUser, actionEntityType, actionEntity) => { };
            AddUserToEntityMapping(user, entityType, entity, postProcessingAction);
        }
        
        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                AddUser(user, true);
                AddEntity(entityType, entity, true);
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, entityType, entity);
            };
            AddUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            Action<TUser, String, String> postProcessingAction = (actionUser, actionEntityType, actionEntity) => { };
            RemoveUserToEntityMapping(user, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(user, entityType, entity);
            };
            RemoveUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Action<TGroup, String, String> postProcessingAction = (actionGroup, actionEntityType, actionEntity) => { };
            AddGroupToEntityMapping(group, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                AddGroup(group, true);
                AddEntity(entityType, entity, true);
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(group, entityType, entity);
            };
            AddGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            Action<TGroup, String, String> postProcessingAction = (actionGroup, actionEntityType, actionEntity) => { };
            RemoveGroupToEntityMapping(group, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                try
                {
                    baseAction.Invoke();
                }
                catch (ArgumentException)
                {
                }
                postProcessingAction.Invoke(group, entityType, entity);
            };
            RemoveGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Idempotently adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected void AddUser(TUser user, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                userToGroupMap.AddLeafVertex(user);
                if (generateEvent == true)
                {
                    eventProcessor.AddUser(user);
                }
            }
        }

        /// <summary>
        /// Idempotently adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected void AddGroup(TGroup group, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
            {
                userToGroupMap.AddNonLeafVertex(group);
                if (generateEvent == true)
                {
                    eventProcessor.AddGroup(group);
                }
            }
        }

        /// <summary>
        /// Idempotently adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected void AddEntityType(String entityType, Boolean generateEvent)
        {
            if (entities.ContainsKey(entityType) == false)
            {
                entities.Add(entityType, collectionFactory.GetSetInstance<String>());
                if (generateEvent == true)
                {
                    eventProcessor.AddEntityType(entityType);
                }
            }
        }

        /// <summary>
        /// Idempotently adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected void AddEntity(String entityType, String entity, Boolean generateEvent)
        {
            AddEntityType(entityType, generateEvent);
            if (entities[entityType].Contains(entity) == false)
            {
                entities[entityType].Add(entity);
                if (generateEvent == true)
                {
                    eventProcessor.AddEntity(entityType, entity);
                }
            }
        }

        #endregion
    }
}

