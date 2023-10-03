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
using System.Collections.Generic;
using System.Runtime.Serialization;

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
    public class DependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>, IDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
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
         *   1) We override protected method with 'wrappingAction' parameter from ConcurrentAccessManager for each type of operation
         *     The general idea here is that this class and any subclasses which override ConcurrentAccessManager just keep 'chaining' their decorated functionality through this method.
         *     The implementations are called 'inside out' as compared to the inheritance hierarchy, so even though ConcurrentAccessManager is lowest in the hierarchy, its locking functionality is called first in the 'wrappingAction' method overloads.
         *     I.e. order of decorated functionality should be: Apply locks (ConcurrentAccessManager) > Do any event prepending and idempotency (DependencyFreeAccessManager) > Log metrics (MetricLoggingDependencyFreeAccessManager)
         *       which moves up the inheritance hierarchy, even though the first call is from the top.
         *   2) Try to always invoke the 'baseAction' action to implement the actual underlying work... better to reuse base class functionality as much as possible (primary add methods are a case where we cannot invoke 'baseAction' as described above)
         *     This is particularly important for primary reomve methods where the base class methods do cleanup of any dependent elements
         *   3) In all pretected methods overloads, the 'wrappingAction' should be invoked.  If it's not, functionality decorated by subclasses (e.g. metric logging) or by the method with the 'postProcessingAction' will not be run.
         *   4) If member thrownIdempotencyExceptions is set true, idempotent Add* methods will throw IdempotentAddOperationException and Remove* methods will throw IdempotentRemoveOperationException.  As per constructor comments, this is so that 
         *        subclasses or composing classes have a way of knowing that the operation was idempotent... specifically this is used in MetricLoggingDependencyFreeAccessManager to cancel any metric logging when an operation is idempotent.  Without
         *        this mechanism MetricLoggingDependencyFreeAccessManager would have no way of knowing that an operation was idempotent, and would log metrics when the event the metric relates to didn't ossur.
         */

        /// <summary>The event processor to pass any depended-on/prepended events to.</summary>
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> eventProcessor;
        /// <summary>Whether idempotent operation exceptions should be thrown when an Add* or Remove* event is idempotent.</summary>
        protected Boolean thrownIdempotencyExceptions = true;

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
        /// <remarks>If parameter <paramref name="storeBidirectionalMappings"/> is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeAccessManager(Boolean storeBidirectionalMappings)
            : base(storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
            thrownIdempotencyExceptions = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="concurrentDirectedGraph">The ConcurrentDirectedGraph instance to use to store users and groups.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <remarks>If parameter <paramref name="storeBidirectionalMappings"/> is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public DependencyFreeAccessManager(ConcurrentDirectedGraph<TUser, TGroup> concurrentDirectedGraph, Boolean storeBidirectionalMappings)
            : base(concurrentDirectedGraph, storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
            thrownIdempotencyExceptions = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="concurrentDirectedGraph">The ConcurrentDirectedGraph instance to use to store users and groups.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="thrownIdempotencyExceptions">Whether idempotent operation exceptions should be thrown when an Add* or Remove* event is idempotent.</param>
        /// <remarks>
        ///   <para>If parameter <paramref name="storeBidirectionalMappings"/> is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</para>
        ///   <para>Setting parameter <paramref name="thrownIdempotencyExceptions"/> to true provides a mechanism for classes which subclass or compose this class to detect when an Add* or Remove* event method is called idempotently, and adjust their logic accordingly.  For example, a metric logging subclass of this class would set the parameter to true, and cancel and metric logging if and idempotent operation exception was caught.</para>
        /// </remarks>
        protected DependencyFreeAccessManager(ConcurrentDirectedGraph<TUser, TGroup> concurrentDirectedGraph, Boolean storeBidirectionalMappings, Boolean thrownIdempotencyExceptions)
            : base(concurrentDirectedGraph, storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
            this.thrownIdempotencyExceptions = thrownIdempotencyExceptions;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.DependencyFreeAccessManager class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="thrownIdempotencyExceptions">Whether idempotent operation exceptions should be thrown when an Add* or Remove* event is idempotent.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public DependencyFreeAccessManager(ICollectionFactory collectionFactory, Boolean storeBidirectionalMappings, Boolean thrownIdempotencyExceptions)
            : base(collectionFactory, storeBidirectionalMappings)
        {
            eventProcessor = new NullAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>();
            this.thrownIdempotencyExceptions = thrownIdempotencyExceptions;
        }

        /// <inheritdoc/>
        public virtual HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups)
        {
            var returnGroups = new HashSet<TGroup>();
            foreach (TGroup currentGroup in groups)
            {
                if (returnGroups.Contains(currentGroup) == false)
                {
                    Func<TGroup, Boolean> vertexAction = (TGroup currentTraversalGroup) =>
                    {
                        if ((returnGroups.Contains(currentTraversalGroup) == false))
                        {
                            returnGroups.Add(currentTraversalGroup);
                        }

                        return true;
                    };
                    try
                    {
                        userToGroupMap.TraverseFromNonLeaf(currentGroup, vertexAction);
                    }
                    catch (NonLeafVertexNotFoundException<TGroup>)
                    {
                        // Ignore and continue if 'currentGroup' doesn't exist in the group
                    }
                }
            }

            return returnGroups;
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToApplicationComponent(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel)
        {
            var comparisonComponentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(applicationComponent, accessLevel);
            foreach (TGroup currentGroup in groups)
            {
                Boolean containsUser = groupToComponentMap.TryGetValue(currentGroup, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
                if (containsUser == true && componentsAndAccessInMapping.Contains(comparisonComponentAndAccess) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToEntity(IEnumerable<TGroup> groups, String entityType, String entity)
        {
            foreach (TGroup currentGroup in groups)
            {
                Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
                if (containsGroup == true)
                {
                    Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(entityType, out ISet<String> entitiesInMapping);
                    if (containsEntity == true && entitiesInMapping.Contains(entity) == true)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #region Private/Protected Methods

        #region Base Class Overrides

        /// <inheritdoc/>
        protected override void AddUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            Action<TUser, Action> idempotentAddAction = (actionUser, baseAction) =>
            {
                // Note 'baseAction' is ignored/unused, as this class overrides the base class action with an idempotent 'Add' operation
                wrappingAction.Invoke(actionUser, () => { AddUser(actionUser, false); });
            };
            base.AddUser(user, idempotentAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            Action<TUser, Action> idempotentRemoveAction = (actionUser, baseAction) =>
            {
                wrappingAction.Invoke(actionUser, () =>
                {
                    if (userToGroupMap.ContainsLeafVertex(actionUser) == true)
                    {
                        baseAction.Invoke();
                    }
                    else
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveUser(user, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            Action<TGroup, Action> idempotentAddAction = (actionGroup, baseAction) =>
            {
                wrappingAction.Invoke(actionGroup, () => { AddGroup(actionGroup, false); });
            };
            base.AddGroup(group, idempotentAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            Action<TGroup, Action> idempotentRemoveAction = (actionGroup, baseAction) =>
            {
                wrappingAction.Invoke(actionGroup, () =>
                {
                    if (userToGroupMap.ContainsNonLeafVertex(actionGroup) == true)
                    {
                        baseAction.Invoke();
                    }
                    else
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveGroup(group, idempotentRemoveAction);
        }
        
        /// <inheritdoc/>
        protected override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            Action<TUser, TGroup, Action> prependedAddAction = (actionUser, actionGroup, baseAction) =>
            {
                // Generate any prepended events before invoking 'wrappingAction', e.g. so the prepended events are not included in metric timing
                try 
                { 
                    AddUser(actionUser, true); 
                }
                catch (IdempotentAddOperationException) 
                {
                    // Ignore any idempotent operation exception for the prepended events
                }
                try 
                { 
                    AddGroup(actionGroup, true); 
                } 
                catch (IdempotentAddOperationException) 
                {
                }
                wrappingAction.Invoke(actionUser, actionGroup, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                });
            };
            base.AddUserToGroupMapping(user, group, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            Action<TUser, TGroup, Action> idempotentRemoveAction = (actionUser, actionGroup, baseAction) =>
            {
                wrappingAction.Invoke(actionUser, actionGroup, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveUserToGroupMapping(user, group, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            if (fromGroup.Equals(toGroup) == true)
                throw new ArgumentException($"Parameters '{nameof(fromGroup)}' and '{nameof(toGroup)}' cannot contain the same group.", nameof(toGroup));

            Action<TGroup, TGroup, Action> prependedAddAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                try
                {
                    AddGroup(actionFromGroup, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                try
                {
                    AddGroup(actionToGroup, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionFromGroup, actionToGroup, () =>
                {
                    try
                    {
                        // Would prefer to call baseAction.Invoke() here, but it doesn't allow distinguishing between NonLeafToNonLeafEdgeAlreadyExistsException (which should be swallowed to
                        //   allow idempotency) and CircularReferenceException (which should be rethrown as it's an error case)
                        userToGroupMap.AddNonLeafToNonLeafEdge(actionFromGroup, actionToGroup);
                    }
                    catch (NonLeafToNonLeafEdgeAlreadyExistsException<TGroup>)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                    catch (CircularReferenceException circularReferenceException)
                    {
                        throw new ArgumentException($"A mapping between groups '{actionFromGroup.ToString()}' and '{actionToGroup.ToString()}' cannot be created as it would cause a circular reference.", nameof(toGroup), circularReferenceException);
                    }
                });
            };
            base.AddGroupToGroupMapping(fromGroup, toGroup, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            Action<TGroup, TGroup, Action> idempotentRemoveAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                wrappingAction.Invoke(actionFromGroup, actionToGroup, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveGroupToGroupMapping(fromGroup, toGroup, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TUser, TComponent, TAccess, Action> prependedAddAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                try
                {
                    AddUser(actionUser, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionUser, actionApplicationComponent, actionAccessLevel, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                });
            };
            base.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TUser, TComponent, TAccess, Action> idempotentRemoveAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                wrappingAction.Invoke(actionUser, actionApplicationComponent, actionAccessLevel, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> prependedAddAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                try
                {
                    AddGroup(actionGroup, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionGroup, actionApplicationComponent, actionAccessLevel, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                });
            };
            base.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> idempotentRemoveAction = (actioGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                wrappingAction.Invoke(actioGroup, actionApplicationComponent, actionAccessLevel, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            Action<String, Action> idempotentAddAction = (actionEntityType, baseAction) =>
            {
                wrappingAction.Invoke(actionEntityType, () => { AddEntityType(actionEntityType, false); });
            };
            base.AddEntityType(entityType, idempotentAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            Action<String, Action> idempotentRemoveAction = (actionEntityType, baseAction) =>
            {
                wrappingAction.Invoke(actionEntityType, () =>
                {
                    if (entities.ContainsKey(actionEntityType) == true)
                    {
                        baseAction.Invoke();
                    }
                    else
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveEntityType(entityType, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        /// <remarks>This must be called with locks provided by base <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> class set, e.g. via the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}.RemoveEntityType(string, Action{string, Action})"/> method.</remarks>
        protected override void RemoveEntityType(String entityType, Action<TUser, String, IEnumerable<String>, Int32> userToEntityTypeMappingPreRemovalAction, Action<TGroup, String, IEnumerable<String>, Int32> groupToEntityTypeMappingPreRemovalAction)
        {
            if (entities.ContainsKey(entityType) == true)
            {
                base.RemoveEntityType(entityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentRemoveOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void AddEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            Action<String, String, Action> idempotentAddAction = (actionEntityType, actionEntity, baseAction) =>
            {
                try
                {
                    AddEntityType(actionEntityType, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionEntityType, actionEntity, () => 
                { 
                    try
                    {
                        AddEntity(actionEntityType, actionEntity, false);
                    }
                    catch (IdempotentAddOperationException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw;
                    }
                });
            };
            base.AddEntity(entityType, entity, idempotentAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            Action<String, String, Action> idempotentRemoveAction = (actionEntityType, actionEntity, baseAction) =>
            {
                wrappingAction.Invoke(actionEntityType, actionEntity, () =>
                {
                    if (entities.ContainsKey(actionEntityType) == true && entities[actionEntityType].Contains(actionEntity) == true)
                    {
                        baseAction.Invoke();
                    }
                    else
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveEntity(entityType, entity, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void RemoveEntity(String entityType, String entity, Action<TUser, String, String> userToEntityMappingPostRemovalAction, Action<TGroup, String, String> groupToEntityMappingPostRemovalAction)
        {
            if (entities.ContainsKey(entityType) == true && entities[entityType].Contains(entity) == true)
            {
                base.RemoveEntity(entityType, entity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentRemoveOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            Action<TUser, String, String, Action> prependedAddAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                try
                {
                    AddUser(actionUser, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                try
                {
                    AddEntity(actionEntityType, actionEntity, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionUser, actionEntityType, actionEntity, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                });
            };
            base.AddUserToEntityMapping(user, entityType, entity, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            Action<TUser, String, String, Action> idempotentRemoveAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                wrappingAction.Invoke(actionUser, actionEntityType, actionEntity, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveUserToEntityMapping(user, entityType, entity, idempotentRemoveAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            Action<TGroup, String, String, Action> prependedAddAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                try
                {
                    AddGroup(actionGroup, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                try
                {
                    AddEntity(actionEntityType, actionEntity, true);
                }
                catch (IdempotentAddOperationException)
                {
                }
                wrappingAction.Invoke(actionGroup, actionEntityType, actionEntity, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentAddOperationException();
                    }
                });
            };
            base.AddGroupToEntityMapping(group, entityType, entity, prependedAddAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            Action<TGroup, String, String, Action> idempotentRemoveAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                wrappingAction.Invoke(actionGroup, actionEntityType, actionEntity, () =>
                {
                    try
                    {
                        baseAction.Invoke();
                    }
                    catch (ArgumentException)
                    {
                        if (thrownIdempotencyExceptions == true)
                            throw new IdempotentRemoveOperationException();
                    }
                });
            };
            base.RemoveGroupToEntityMapping(group, entityType, entity, idempotentRemoveAction);
        }

        #endregion

        #region Idempotent 'Add' Methods for Primary Elements

        /// <summary>
        /// Idempotently adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected virtual void AddUser(TUser user, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                userToGroupMap.AddLeafVertex(user);
                if (generateEvent == true)
                {
                    eventProcessor.AddUser(user);
                }
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <summary>
        /// Idempotently adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected virtual void AddGroup(TGroup group, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
            {
                userToGroupMap.AddNonLeafVertex(group);
                if (generateEvent == true)
                {
                    eventProcessor.AddGroup(group);
                }
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <summary>
        /// Idempotently adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected virtual void AddEntityType(String entityType, Boolean generateEvent)
        {
            if (entities.ContainsKey(entityType) == false)
            {
                entities.Add(entityType, collectionFactory.GetSetInstance<String>());
                if (generateEvent == true)
                {
                    eventProcessor.AddEntityType(entityType);
                }
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <summary>
        /// Idempotently adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        protected virtual void AddEntity(String entityType, String entity, Boolean generateEvent)
        {
            try
            {
                AddEntityType(entityType, generateEvent);
            }
            catch (IdempotentAddOperationException)
            {
            }
            if (entities[entityType].Contains(entity) == false)
            {
                entities[entityType].Add(entity);
                if (generateEvent == true)
                {
                    eventProcessor.AddEntity(entityType, entity);
                }
            }
            else
            {
                if (thrownIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        #endregion

        #endregion
    }
}

